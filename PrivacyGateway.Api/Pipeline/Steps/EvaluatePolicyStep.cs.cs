using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;
using PrivacyGateway.Api.Services;

namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class EvaluatePolicyStep : IPipelineStep
{
    private readonly PolicyEngine _policy;

    public EvaluatePolicyStep(PolicyEngine policy) => _policy = policy;

    public string Name => "EvaluatePolicy";
    public int Order => 30;
    public PipelineMode AppliesTo => PipelineMode.Analyze | PipelineMode.Chat;

    public Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        var decision = _policy.Decide(context.Findings);

        context.Decision = decision.Decision;
        context.Reason = decision.Reason;
        context.RequiredSanitizationTypes = decision.RequiredSanitizationTypes;

        return Task.CompletedTask;
    }
}