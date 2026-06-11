namespace PrivacyGateway.Api.Models;

// ── Enums 

public enum CoverageLevel
{
    Covered,
    Partial,
    NotCovered
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

// ── System control: feature → GDPR-artikel(er) 

public sealed record GdprSystemControl(
    string Id,
    string Feature,
    string Component,
    string[] Articles,
    string[] Principles,
    string Description,
    CoverageLevel Coverage
);

// ── PII-type klassifikation 

public sealed record GdprPiiClassification(
    string PiiType,
    string Category,        // fx "Almindelige personoplysninger" / "Særlige kategorier"
    string[] Articles,
    string[] Principles,
    string PolicyOutcome,   // ALLOW+SANITIZE eller BLOCK
    RiskLevel RiskLevel,
    string Rationale
);

// ── Samlet svar fra /gdpr/map 

public sealed record GdprMapResponse(
    IReadOnlyList<GdprSystemControl> SystemControls,
    IReadOnlyList<GdprPiiClassification> PiiClassifications,
    GdprCoverageSummary Summary
);

public sealed record GdprCoverageSummary(
    int TotalControls,
    int Covered,
    int Partial,
    int NotCovered,
    string[] CoveredArticles
);
