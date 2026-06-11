using System.ClientModel;
using OpenAI.Chat;
using PrivacyGateway.Api.Models;
using PrivacyGateway.Api.Services;
using PrivacyGateway.Api.Pipeline;
using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;
using PrivacyGateway.Api.Pipeline.Steps;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Vores services
builder.Services.AddSingleton<PiiDetector>();
builder.Services.AddSingleton<Sanitizer>();
builder.Services.AddSingleton<PolicyEngine>();
builder.Services.AddSingleton<AuditService>();
builder.Services.AddSingleton<FileTextExtractor>(); // Fil-tekst ekstraktion (PDF, TXT, MD, CSV)

// OutputGuard konfiguration: hvilke PII-typer skal vi kigge efter i AI-svaret, og hvilke skal vi blokere på?
builder.Services.AddSingleton(new OutputGuardOptions
{
    BlockedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PiiDetector.Email,
        PiiDetector.Phone,
        PiiDetector.Cpr,
        PiiDetector.Iban,
        PiiDetector.CreditCard,
        PiiDetector.Passport,
        PiiDetector.DkAccount,
        PiiDetector.IpAddress,
        PiiDetector.Url,
        PiiDetector.Cvr,
    },
    BlockOnCpr = true,
    BlockOnPassport = true,
    BlockOnCreditCard = true,
    BlockOnDkAccount = true,
    BlockOnIban = true

});

// Pipeline og steps
builder.Services.AddSingleton<IngestionPipeline>();
builder.Services.AddSingleton<IPipelineStep, ExtractTextStep>();
builder.Services.AddSingleton<IPipelineStep, DetectPiiStep>();
builder.Services.AddSingleton<IPipelineStep, EvaluatePolicyStep>();
builder.Services.AddSingleton<IPipelineStep, SanitizeStep>();
builder.Services.AddSingleton<IPipelineStep, EnforceStep>();
builder.Services.AddSingleton<IPipelineStep, CallAiStep>();
builder.Services.AddSingleton<IPipelineStep, OutputGuardStep>();
builder.Services.AddSingleton<GdprMappingService>();

// OpenAI ChatClient (valgfri: app kan starte uden key, så /analyze virker)
string model =
    builder.Configuration["OpenAI:Model"]
    ?? Environment.GetEnvironmentVariable("OPENAI_MODEL")
    ?? "gpt-4o-mini";

// Prøv først at hente API-nøglen fra konfiguration (appsettings, user secrets, env vars osv.) — så fallback til direkte miljøvariabel
string? apiKey =
    builder.Configuration["OpenAI:ApiKey"]
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
// Hvis der er en API-nøgle, så registrer ChatClient i DI — ellers starter appen uden AI-integration
if (!string.IsNullOrWhiteSpace(apiKey))
    builder.Services.AddSingleton(new ChatClient(model, new ApiKeyCredential(apiKey)));

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Redirect("/swagger"));

// ── POST /analyze 
app.MapPost("/analyze", async (
    AnalyzeRequest req,
    IngestionPipeline pipeline,
    AuditService audit,
    CancellationToken ct) =>
{
    var context = new PipelineContext(PipelineMode.Analyze, req.Text);
    await pipeline.RunAsync(context, ct);
    audit.Record(context);

    if (context.FailureStatusCode.HasValue)
        return Results.Problem(
            title: "Pipeline error",
            detail: string.Join("; ", context.Errors),
            statusCode: context.FailureStatusCode.Value);

    return Results.Ok(new AnalyzeResponse(
        Decision: context.Decision,
        Reason: context.Reason,
        SanitizedText: context.SanitizedText,
        Findings: context.Findings));
});

// ── POST /analyze/files 
// Accepterer én eller flere filer (multipart/form-data).
// Understøtter: .pdf, .txt, .md, .csv, .log
// Returnerer et array af FileAnalyzeResult — ét pr. fil.
app.MapPost("/analyze/files", async (
    HttpRequest request,
    IngestionPipeline pipeline,
    AuditService audit,
    FileTextExtractor extractor,
    CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data.");

    var form = await request.ReadFormAsync(ct);
    var files = form.Files;

    if (files.Count == 0)
        return Results.BadRequest("Ingen filer uploadet.");

    const long maxBytes = 10 * 1024 * 1024; // 10 MB pr. fil

    var results = new List<FileAnalyzeResult>(files.Count);

    foreach (var file in files)
    {
        var name = file.FileName ?? "ukendt";

        // Tjek størrelse
        if (file.Length > maxBytes)
        {
            results.Add(FileError(name, file.Length,
                $"Filen er for stor ({file.Length / 1024.0 / 1024:F1} MB). Maks er 10 MB."));
            continue;
        }

        // Ekstraher tekst
        await using var stream = file.OpenReadStream();
        var (text, extractError) = extractor.Extract(stream, name);

        if (extractError is not null)
        {
            results.Add(FileError(name, file.Length, extractError));
            continue;
        }

        // Kør pipeline
        var context = new PipelineContext(PipelineMode.Analyze, text);
        await pipeline.RunAsync(context, ct);
        audit.Record(context);

        if (context.FailureStatusCode.HasValue)
        {
            results.Add(FileError(name, file.Length, string.Join("; ", context.Errors)));
            continue;
        }

        results.Add(new FileAnalyzeResult(
            FileName: name,
            FileSizeBytes: file.Length,
            ExtractError: null,
            Decision: context.Decision,
            Reason: context.Reason,
            SanitizedText: context.SanitizedText,
            Findings: context.Findings));
    }

    return Results.Ok(results);
});

// ── POST /chat 
app.MapPost("/chat", async (
    ChatRequest req,
    IngestionPipeline pipeline,
    AuditService audit,
    CancellationToken ct) =>
{
    var context = new PipelineContext(PipelineMode.Chat, req.Text);
    await pipeline.RunAsync(context, ct);
    audit.Record(context);

    // Hvis pipeline fejlede (fx pga. OpenAI-fejl, eller fordi OutputGuard blokerede AI-svaret), så returner en 400/500 med fejlbeskederne i response body.
    if (context.FailureStatusCode.HasValue)
        return Results.Problem(
            title: "Pipeline error",
            detail: string.Join("; ", context.Errors),
            statusCode: context.FailureStatusCode.Value);

    // Hvis pipeline lykkedes, returner AI-svaret + metadata om beslutning, fundne PII'er osv.
    return Results.Ok(new ChatResponse(
        Decision: context.Decision,
        Reason: context.Reason,
        SanitizedText: context.SanitizedText,
        Findings: context.Findings,
        AiResponse: context.AiResponse));
});

// ── GET /audit 
app.MapGet("/audit", (AuditService audit) =>
    Results.Ok(audit.GetAll().OrderByDescending(e => e.Timestamp)));

app.MapGet("/gdpr/map", (GdprMappingService gdpr) =>
    Results.Ok(gdpr.BuildMap()));

app.Run();

// ── Helpers 
static FileAnalyzeResult FileError(string name, long size, string error) =>
    new(FileName: name,
        FileSizeBytes: size,
        ExtractError: error,
        Decision: "ERROR",
        Reason: error,
        SanitizedText: string.Empty,
        Findings: Array.Empty<Finding>());