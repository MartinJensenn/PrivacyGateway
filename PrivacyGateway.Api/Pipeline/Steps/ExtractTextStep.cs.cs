using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;

namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class ExtractTextStep : IPipelineStep
{
    public string Name => "ExtractText";
    public int Order => 10;
    public PipelineMode AppliesTo => PipelineMode.Analyze | PipelineMode.Chat;

    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.RawInput))
        {
            context.Fail(400, "Text is required.");
            return Task.CompletedTask;
        }

        // MVP: tekst-input -> extracted text
        context.ExtractedText = context.RawInput;
        return Task.CompletedTask;
    }
}