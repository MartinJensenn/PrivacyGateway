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

    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        if (context.RequiredSanitizationTypes.Count == 0)
        {
            context.SanitizedText = context.ExtractedText;
            return Task.CompletedTask;
        }

        // Sanitér kun de findings som policy kræver
        var toSanitize = context.Findings
            .Where(f => context.RequiredSanitizationTypes.Contains(f.Type))
            .ToList();

        context.SanitizedText = _sanitizer.Sanitize(context.ExtractedText, toSanitize);
        return Task.CompletedTask;
    }
}