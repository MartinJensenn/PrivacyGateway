using PrivacyGateway.Api.Models;

namespace PrivacyGateway.Api.Services;

public sealed class PolicyEngine
{
    public sealed record PolicyDecision(
        string Decision,
        string Reason,
        IReadOnlySet<string> RequiredSanitizationTypes);

    public PolicyDecision Decide(IReadOnlyList<Finding> findings)
    {
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // MVP: CPR => BLOCK
        if (findings.Any(f => string.Equals(f.Type, PiiDetector.Cpr, StringComparison.OrdinalIgnoreCase)))
        {
            return new PolicyDecision(
                Decision: "BLOCK",
                Reason: "CPR detected (MVP policy)",
                RequiredSanitizationTypes: required
            );
        }

        // Hvis EMAIL/PHONE findes, så ALLOW men kræv sanitization
        if (findings.Any(f => string.Equals(f.Type, PiiDetector.Email, StringComparison.OrdinalIgnoreCase)))
            required.Add(PiiDetector.Email);

        if (findings.Any(f => string.Equals(f.Type, PiiDetector.Phone, StringComparison.OrdinalIgnoreCase)))
            required.Add(PiiDetector.Phone);

        var reason = required.Count > 0
            ? $"PII detected; sanitization required: {string.Join(", ", required)}"
            : "No PII detected";

        return new PolicyDecision(
            Decision: "ALLOW",
            Reason: reason,
            RequiredSanitizationTypes: required
        );
    }
}