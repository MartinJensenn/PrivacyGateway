using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;
using PrivacyGateway.Api.Services;

namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class SanitizeStep : IPipelineStep
{
    private readonly Sanitizer _sanitizer;

    public SanitizeStep(Sanitizer sanitizer) => _sanitizer = sanitizer;

    public string Name => "Sanitize";
    public int Order => 40;
    public PipelineMode AppliesTo => PipelineMode.Analyze | PipelineMode.Chat;

    // SanitizeStep erstatter PII-segmenter i den udtrukne tekst med tokens, baseret på de fundne findings og de typer, der kræves sanitization for
    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        // Hvis der ikke er nogen typer, der kræver sanitization, skal jeg ikke ændre den udtrukne tekst
        if (context.RequiredSanitizationTypes.Count == 0)
        {
            // Ingen sanitization krævet, brug den udtrukne tekst som den er
            context.SanitizedText = context.ExtractedText;
            return Task.CompletedTask;
        }

        // Sanitér kun de findings som policy kræver
        var toSanitize = context.Findings
            .Where(f => context.RequiredSanitizationTypes.Contains(f.Type))
            .ToList();

        // Brug Sanitizer til at erstatte PII-segmenter i den udtrukne tekst med tokens
        context.SanitizedText = _sanitizer.Sanitize(context.ExtractedText, toSanitize);
        return Task.CompletedTask;
    }
}