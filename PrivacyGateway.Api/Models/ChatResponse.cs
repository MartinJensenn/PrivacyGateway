namespace PrivacyGateway.Api.Models;

public sealed record ChatResponse(
    string Decision,
    string Reason,
    string SanitizedText,
    IReadOnlyList<Finding> Findings,
    string? AiResponse
);