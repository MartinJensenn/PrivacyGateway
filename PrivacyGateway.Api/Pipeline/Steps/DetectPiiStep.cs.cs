using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;
using PrivacyGateway.Api.Services;

namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class DetectPiiStep : IPipelineStep
{
    private readonly PiiDetector _detector;

    public DetectPiiStep(PiiDetector detector) => _detector = detector;

    public string Name => "DetectPii";
    public int Order => 20;
    public PipelineMode AppliesTo => PipelineMode.Analyze | PipelineMode.Chat;

    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        context.Findings = _detector.FindAll(context.ExtractedText);
        return Task.CompletedTask;
    }
}