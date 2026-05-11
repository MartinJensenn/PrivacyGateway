using PrivacyGateway.Api.Models;

namespace PrivacyGateway.Api.Services;

public sealed class PolicyEngine
{
    public sealed record PolicyDecision(
        string Decision,
        string Reason,
        IReadOnlySet<string> RequiredSanitizationTypes);

    // PII-typer der altid medfører BLOCK, uanset hvad der ellers er i teksten
    private static readonly IReadOnlySet<string> BlockedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {

    };

    // PII-typer der medfører ALLOW med påkrævet sanitering
    private static readonly IReadOnlySet<string> SanitizeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PiiDetector.Email,
        PiiDetector.Phone,
        PiiDetector.Cvr,
        PiiDetector.IpAddress,
        PiiDetector.Url,
        PiiDetector.Cpr,
        PiiDetector.Iban,
        PiiDetector.CreditCard,
        PiiDetector.Passport,
        PiiDetector.DkAccount,
    };

    public PolicyDecision Decide(IReadOnlyList<Finding> findings)
    {
        if (findings.Count == 0)
        {
            return new PolicyDecision(
                Decision: "ALLOW",
                Reason: "No PII detected",
                RequiredSanitizationTypes: new HashSet<string>()
            );
        }

        var foundTypes = findings
            .Select(f => f.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Tjek for BLOCK-typer — beskriv præcis hvilke der udløste blokering
        var blockedFound = foundTypes
            .Where(t => BlockedTypes.Contains(t))
            .OrderBy(t => t)
            .ToList();

        
        if (blockedFound.Count > 0)
        {
            // Hvis der er fundet BLOCK-typer, så blokér og angiv præcis hvilke typer der udløste blokeringen
            var reason = blockedFound.Count == 1
                ? $"{blockedFound[0]} detected — request blocked by policy"
                : $"Multiple sensitive types detected ({string.Join(", ", blockedFound)}) — request blocked by policy";

            // Blokér uden sanitering, da BLOCK-typer ikke kan eller skal saniteres
            return new PolicyDecision(
                Decision: "BLOCK",
                Reason: reason,
                RequiredSanitizationTypes: new HashSet<string>()
            );
        }

        // Saml de typer som kræver sanitering
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in foundTypes.Where(t => SanitizeTypes.Contains(t)))
            required.Add(type);

        // Hvis der ikke er fundet BLOCK-typer, så tillad med påkrævet sanitering, og angiv præcis hvilke typer der kræver sanitering
        var sanitizeReason = required.Count > 0
            ? $"PII detected — sanitization required: {string.Join(", ", required.OrderBy(t => t))}"
            : "No actionable PII detected";

        // Tillad med sanitering, hvis der er fundet typer som kræver det, ellers tillad uden sanitering
        return new PolicyDecision(
            Decision: "ALLOW",
            Reason: sanitizeReason,
            RequiredSanitizationTypes: required
        );
    }
}