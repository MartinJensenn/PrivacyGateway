using System.Collections.Concurrent;
using PrivacyGateway.Api.Models;
using PrivacyGateway.Api.Pipeline.Context;

namespace PrivacyGateway.Api.Services;

public sealed class AuditService
{
    // ConcurrentQueue er trådsikker uden lock — vigtig da API'et håndterer requests parallelt
    private readonly ConcurrentQueue<AuditEntry> _entries = new();

    // Denne metode kaldes af PipelineContext i slutningen af hver pipeline-eksekvering
    public void Record(PipelineContext context)
    {
        var piiTypes = context.Findings
            .Select(f => f.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        // Opretter en audit entry med relevante oplysninger fra pipeline-konteksten
        var entry = new AuditEntry(
            CorrelationId: context.CorrelationId,
            Timestamp: DateTimeOffset.UtcNow,
            Mode: context.Mode.ToString(),
            Decision: context.Decision,
            Reason: context.Reason,
            PiiTypesFound: piiTypes
        );

        _entries.Enqueue(entry);
    }

    public IReadOnlyList<AuditEntry> GetAll() => _entries.ToArray();
}