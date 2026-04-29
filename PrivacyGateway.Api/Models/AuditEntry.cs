namespace PrivacyGateway.Api.Models;

public sealed record AuditEntry(
    Guid CorrelationId,
    DateTimeOffset Timestamp,
    string Mode,
    string Decision,
    string Reason,
    IReadOnlyList<string> PiiTypesFound
);