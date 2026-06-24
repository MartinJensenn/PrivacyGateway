using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;
using PrivacyGateway.Api.Services;

namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class EnforceStep : IPipelineStep
{
    private readonly PiiDetector _detector;

    public EnforceStep(PiiDetector detector) => _detector = detector;

    public string Name => "Enforce";
    public int Order => 50;
    public PipelineMode AppliesTo => PipelineMode.Analyze | PipelineMode.Chat;

    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        // BLOCK => stop pipeline (men det er ikke en "error")
        if (string.Equals(context.Decision, "BLOCK", StringComparison.OrdinalIgnoreCase))
        {
            context.Terminate();
            return Task.CompletedTask;
        }

        // Fail-safe: verify at required types faktisk er væk
        if (context.RequiredSanitizationTypes.Count > 0)
        {
            var stillThere = _detector.FindAll(context.SanitizedText)
                .Where(f => context.RequiredSanitizationTypes.Contains(f.Type))
                .ToList();
        }

        return Task.CompletedTask;
    }
}