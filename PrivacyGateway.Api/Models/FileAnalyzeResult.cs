namespace PrivacyGateway.Api.Models;

public sealed record FileAnalyzeResult(
    string FileName,
    long FileSizeBytes,
    string? ExtractError,
    string Decision,
    string Reason,
    string SanitizedText,
    IReadOnlyList<Finding> Findings
);
