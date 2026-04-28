using PrivacyGateway.Api.Pipeline.Context;

namespace PrivacyGateway.Api.Pipeline.Abstractions;

public interface IPipelineStep
{
    string Name { get; }
    int Order { get; }
    PipelineMode AppliesTo { get; }

    // The method is asynchronous and returns a Task, which allows for non-blocking execution of potentially long-running operations within the pipeline step.
    Task ExecuteAsync(PipelineContext context, CancellationToken ct);
}
 