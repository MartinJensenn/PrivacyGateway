namespace PrivacyGateway.Api.Models;

public sealed record AnalyzeResponse(
    string Decision,
    string Reason,
    string SanitizedText,
    IReadOnlyList<Finding> Findings
);