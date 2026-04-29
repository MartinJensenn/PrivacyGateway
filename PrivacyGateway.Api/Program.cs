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

// Vores services
builder.Services.AddSingleton<PiiDetector>();
builder.Services.AddSingleton<Sanitizer>();
builder.Services.AddSingleton<PolicyEngine>();

builder.Services.AddSingleton(new OutputGuardOptions
{
    BlockedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "EMAIL", "PHONE", "CPR"
    },
    BlockOnCpr = true
});

// Pipeline og steps
builder.Services.AddSingleton<IngestionPipeline>();

// Registr�r steps i r�kkef�lge (order bestemmes af IPipelineStep.Order)
builder.Services.AddSingleton<IPipelineStep, ExtractTextStep>();
builder.Services.AddSingleton<IPipelineStep, DetectPiiStep>();
builder.Services.AddSingleton<IPipelineStep, EvaluatePolicyStep>();
builder.Services.AddSingleton<IPipelineStep, SanitizeStep>();
builder.Services.AddSingleton<IPipelineStep, EnforceStep>();

// CallAiStep er "valgfri" i den forstand, at hvis den ikke er registreret, s� vil /chat stadig virke - det vil bare returnere en fejlmeddelelse om at AI ikke er konfigureret, i stedet for at crashe eller returnere 500.
builder.Services.AddSingleton<IPipelineStep, CallAiStep>();
builder.Services.AddSingleton<IPipelineStep, OutputGuardStep>(); // Til output guard

// OpenAI ChatClient (valgfri: app kan starte uden key, s� /analyze virker)
string model =
    builder.Configuration["OpenAI:Model"]
    ?? Environment.GetEnvironmentVariable("OPENAI_MODEL")
    ?? "gpt-4o-mini";

string? apiKey =
    builder.Configuration["OpenAI:ApiKey"]
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (!string.IsNullOrWhiteSpace(apiKey))
{
    builder.Services.AddSingleton(new ChatClient(model, new ApiKeyCredential(apiKey)));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Hurtig redirect til Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// POST /analyze: scanner + sanitizer + policy (ingen AI-kald)
app.MapPost("/analyze", (AnalyzeRequest req, PiiDetector detector, Sanitizer sanitizer, PolicyEngine policy) =>
{
    var findings = detector.FindAll(req.Text);
    var sanitized = sanitizer.Sanitize(req.Text, findings);
    var policyDecision = policy.Decide(findings);

    return Results.Ok(new AnalyzeResponse(
        Decision: policyDecision.Decision,
        Reason: policyDecision.Reason,
        SanitizedText: sanitized,
        Findings: findings
    ));
});

// POST /chat: samme pipeline, men kun AI-kald hvis ALLOW
app.MapPost("/chat", async (ChatRequest req, IServiceProvider services, PiiDetector detector, Sanitizer sanitizer, PolicyEngine policy) =>
{
    var findings = detector.FindAll(req.Text);
    var sanitized = sanitizer.Sanitize(req.Text, findings);
    var policyDecision = policy.Decide(findings);

    if (policyDecision.Decision == "BLOCK")
    {
        return Results.Ok(new ChatResponse(
            Decision: policyDecision.Decision,
            Reason: policyDecision.Reason,
            SanitizedText: sanitized,
            Findings: findings,
            AiResponse: null
        ));
    }

    var client = services.GetService<ChatClient>();
    if (client is null)
    {
        return Results.Problem(
            title: "OpenAI not configured",
            detail: "Set OPENAI_API_KEY (env var) or OpenAI:ApiKey in appsettings.json to enable /chat."
        );
    }

    // Send KUN sanitized text til AI
    var completion = await client.CompleteChatAsync(sanitized);
    var aiText = completion.Value.Content.FirstOrDefault()?.Text ?? "";

    return Results.Ok(new ChatResponse(
        Decision: policyDecision.Decision,
        Reason: policyDecision.Reason,
        SanitizedText: sanitized,
        Findings: findings,
        AiResponse: aiText
    ));
});

app.Run();