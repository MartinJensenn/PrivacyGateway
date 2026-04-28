using PrivacyGateway.Api.Models;

namespace PrivacyGateway.Api.Pipeline.Context;

public sealed class PipelineContext
{
    public PipelineContext(PipelineMode mode, string rawInput)
    {
        Mode = mode;
        RawInput = rawInput ?? string.Empty;
        CorrelationId = Guid.NewGuid();
    }

    public PipelineMode Mode { get; }
    public Guid CorrelationId { get; }

    // Input
    public string RawInput { get; }

    // Working data
    public string ExtractedText { get; set; } = string.Empty;
    public IReadOnlyList<Finding> Findings { get; set; } = Array.Empty<Finding>();

    // Policy result
    public string Decision { get; set; } = "ALLOW";
    public string Reason { get; set; } = string.Empty;
    public IReadOnlySet<string> RequiredSanitizationTypes { get; set; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Sanitized output
    public string SanitizedText { get; set; } = string.Empty;

    // AI output (kun Chat)
    public string? AiResponse { get; set; }

    // Control / errors
    public bool IsTerminated { get; private set; }
    public int? FailureStatusCode { get; private set; }
    public List<string> Errors { get; } = new();

    public void Terminate() => IsTerminated = true;

    public void Fail(int statusCode, string error)
    {
        FailureStatusCode = statusCode;
        Errors.Add(error);
        IsTerminated = true;
    }

    public bool AppliesTo(PipelineMode mode) => (Mode & mode) != 0;
}