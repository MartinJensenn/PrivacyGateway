using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;

namespace PrivacyGateway.Api.Pipeline;

public sealed class IngestionPipeline
{
    private readonly IReadOnlyList<IPipelineStep> _orderedSteps;

    public IngestionPipeline(IEnumerable<IPipelineStep> steps)
    {
        _orderedSteps = steps
            .OrderBy(s => s.Order)
            .ToList();
    }

    public async Task RunAsync(PipelineContext context, CancellationToken ct)
    {
        foreach (var step in _orderedSteps)
        {
            if (context.IsTerminated) break;

            // Kør kun steps der matcher mode (Analyze/Chat)
            if ((step.AppliesTo & context.Mode) == 0) continue;

            await step.ExecuteAsync(context, ct);
        }
    }
}