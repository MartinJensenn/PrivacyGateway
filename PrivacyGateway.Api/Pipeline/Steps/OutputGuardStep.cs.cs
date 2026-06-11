using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;
using PrivacyGateway.Api.Services;

namespace PrivacyGateway.Api.Pipeline.Steps;

// OutputGuardStep kører efter AI-svaret er modtaget, og tjekker om svaret indeholder PII, som jeg ikke vil have lækket.
public sealed class OutputGuardStep : IPipelineStep
{
    private readonly PiiDetector _detector;
    private readonly Sanitizer _sanitizer;
    private readonly ILogger<OutputGuardStep> _logger;
    private readonly OutputGuardOptions _options;

    // Konstruktor tager de nødvendige services og options via dependency injection
    public OutputGuardStep(
        PiiDetector detector,
        Sanitizer sanitizer,
        ILogger<OutputGuardStep> logger,
        OutputGuardOptions options)
    {
        _detector = detector;
        _sanitizer = sanitizer;
        _logger = logger;
        _options = options;
    }

    public string Name => "OutputGuard";
    public int Order => 70;
    public PipelineMode AppliesTo => PipelineMode.Chat;

    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        if (context.IsTerminated) return Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(context.AiResponse)) return Task.CompletedTask;

        // Find alle PII-findings i AI-svaret
        var allFindings = _detector.FindAll(context.AiResponse);

        // Filtrer kun de typer som OutputGuard er konfigureret til at håndtere
        var relevantFindings = allFindings
            .Where(f => _options.BlockedTypes.Contains(f.Type))
            .ToList();

        if (relevantFindings.Count == 0) return Task.CompletedTask;

        // Log en advarsel — AI'en forsøgte at lække PII
        _logger.LogWarning(
            "[OutputGuard] CorrelationId={CorrelationId} — AI-svar indeholdt {Count} PII-fund: {Types}",
            context.CorrelationId,
            relevantFindings.Count,
            string.Join(", ", relevantFindings.Select(f => f.Type).Distinct())
        );

        // Tjek om nogen af de relevante findings er af en type, som jeg skal blokere på
        var blockTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [PiiDetector.Cpr] = _options.BlockOnCpr,
            [PiiDetector.Passport] = _options.BlockOnPassport,
            [PiiDetector.CreditCard] = _options.BlockOnCreditCard,
            [PiiDetector.Iban] = _options.BlockOnIban,
            [PiiDetector.DkAccount] = _options.BlockOnDkAccount,
        };

        if (relevantFindings.Any(f => blockTypes.TryGetValue(f.Type, out var block) && block))
        {
            // Log en fejl — AI'en forsøgte at lække følsomme personoplysninger
            context.AiResponse = "[SVAR BLOKERET: AI-svaret indeholdt følsomme personoplysninger]";
            return Task.CompletedTask;
        };        

        // Ellers: sanitér de relevante findings
        context.AiResponse = _sanitizer.Sanitize(context.AiResponse, relevantFindings);
        return Task.CompletedTask;
    }
}