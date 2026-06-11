using PrivacyGateway.Api.Models;

namespace PrivacyGateway.Api.Services;

/// <summary>
/// Indeholder den statiske GDPR-mapping for Privacy Gateway.
/// Mapper systemfeatures og PII-typer til relevante GDPR-artikler og -principper.
/// </summary>
public sealed class GdprMappingService
{
    // ── System controls 

    private static readonly GdprSystemControl[] SystemControls =
    [
        new(
            Id: "pii-detection",
            Feature: "PII-detektion",
            Component: "DetectPiiStep · PiiDetector",
            Articles: ["Art. 4(1)", "Art. 5(1)(c)"],
            Principles: ["Dataminimering"],
            Description: "Systemet scanner al tekst for 10 PII-typer (EMAIL, PHONE, CPR, IBAN, " +
                         "CREDIT_CARD, PASSPORT, DK_ACCOUNT, CVR, IP_ADDRESS, URL) inden noget sendes videre. " +
                         "Overlappende matches håndteres ved at beholde det længste/bedste match.",
            Coverage: CoverageLevel.Covered
        ),

        new(
            Id: "pseudonymisation",
            Feature: "Pseudonymisering / Sanitering",
            Component: "SanitizeStep · Sanitizer",
            Articles: ["Art. 4(5)", "Art. 5(1)(c)", "Art. 32(1)(a)"],
            Principles: ["Dataminimering", "Integritet og fortrolighed"],
            Description: "Personoplysninger erstattes med strukturerede tokens (<EMAIL_1>, <PHONE_1> osv.) " +
                         "inden AI-kaldet. Sanitering sker kun for ALLOW-requests — ingen rå personoplysninger " +
                         "sendes til AI-tjenesten.",
            Coverage: CoverageLevel.Covered
        ),

        new(
            Id: "policy-enforcement",
            Feature: "Policy-håndhævelse (ALLOW / BLOCK)",
            Component: "EvaluatePolicyStep · PolicyEngine · EnforceStep",
            Articles: ["Art. 5(1)(b)", "Art. 9(1)", "Art. 32"],
            Principles: ["Formålsbegrænsning", "Særlige kategorier"],
            Description: "Requests der indeholder kritiske PII-typer (CPR, IBAN, kreditkort, pas, DK-konto) " +
                         "blokeres automatisk på inputniveau — teksten når aldrig AI-tjenesten. " +
                         "Øvrige PII-typer medfører ALLOW med påkrævet sanitering.",
            Coverage: CoverageLevel.Covered
        ),

        new(
            Id: "output-guard",
            Feature: "OutputGuard (AI-svar scanning)",
            Component: "OutputGuardStep · OutputGuardOptions",
            Articles: ["Art. 5(1)(f)", "Art. 32"],
            Principles: ["Integritet og fortrolighed"],
            Description: "Ekstra forsvarslag: AI-modellens svar scannes for PII inden det returneres til brugeren. " +
                         "Svar der indeholder kritiske typer blokeres, øvrige fund saniteres. " +
                         "Sikrer at AI ikke lækker oplysninger selv ved uventet modeladfærd.",
            Coverage: CoverageLevel.Covered
        ),

        new(
            Id: "audit-log",
            Feature: "Audit-log uden rå tekst",
            Component: "AuditService",
            Articles: ["Art. 5(2)", "Art. 24"],
            Principles: ["Ansvarlighed"],
            Description: "Alle beslutninger logges med tidsstempel, mode, decision og PII-typer — " +
                         "men aldrig den rå inputtekst. Dette opfylder dokumentationspligten uden ny eksponering.",
            Coverage: CoverageLevel.Covered
        ),

        new(
            Id: "api-key-backend",
            Feature: "AI API-nøgle kun på backend",
            Component: "Program.cs · CallAiStep",
            Articles: ["Art. 32(1)(b)", "Art. 32(2)"],
            Principles: ["Integritet og fortrolighed"],
            Description: "OpenAI API-nøglen konfigureres udelukkende på server-siden. " +
                         "Frontend kan aldrig tilgå eller eksponere nøglen — reducerer risiko for uautoriseret AI-adgang.",
            Coverage: CoverageLevel.Covered
        ),

        new(
            Id: "correlation-tracking",
            Feature: "Correlation ID pr. request",
            Component: "PipelineContext",
            Articles: ["Art. 5(2)", "Art. 24"],
            Principles: ["Ansvarlighed"],
            Description: "Hvert request tildeles et unikt CorrelationId, der muliggør sporbarhed " +
                         "på tværs af pipeline-steps uden at gemme personoplysninger.",
            Coverage: CoverageLevel.Covered
        ),

        new(
            Id: "data-retention",
            Feature: "Dataminimering i lagring",
            Component: "AuditService (in-memory)",
            Articles: ["Art. 5(1)(e)"],
            Principles: ["Opbevaringsbegrænsning"],
            Description: "Audit-loggen er in-memory og nulstilles ved genstart. " +
                         "Der er ikke implementeret formel opbevaringspolitik eller sletterutine.",
            Coverage: CoverageLevel.Partial
        ),
    ];

    // ── PII-type klassifikationer 

    private static readonly GdprPiiClassification[] PiiClassifications =
    [
        new(
            PiiType: "EMAIL",
            Category: "Almindelige personoplysninger",
            Articles: ["Art. 4(1)", "Art. 5(1)(c)"],
            Principles: ["Dataminimering"],
            PolicyOutcome: "ALLOW + SANITIZE",
            RiskLevel: RiskLevel.Medium,
            Rationale: "E-mailadresser er personoplysninger efter art. 4(1). " +
                       "Systemet erstatter dem med <EMAIL_N>-tokens inden AI-kald."
        ),

        new(
            PiiType: "PHONE",
            Category: "Almindelige personoplysninger",
            Articles: ["Art. 4(1)", "Art. 5(1)(c)"],
            Principles: ["Dataminimering"],
            PolicyOutcome: "ALLOW + SANITIZE",
            RiskLevel: RiskLevel.Medium,
            Rationale: "Telefonnumre kan identificere en fysisk person og er personoplysninger. " +
                       "Saniteres til <PHONE_N> inden viderebehandling."
        ),

        new(
            PiiType: "CPR",
            Category: "Særligt følsomme personoplysninger",
            Articles: ["Art. 4(1)", "Art. 87", "Databeskyttelseslovens § 11"],
            Principles: ["Formålsbegrænsning", "Dataminimering"],
            PolicyOutcome: "BLOCK",
            RiskLevel: RiskLevel.Critical,
            Rationale: "Det danske CPR-nummer er en national identifikator med særlig beskyttelse. " +
                       "Databeskyttelseslovens § 11 begrænser kraftigt behandlingsadgangen. " +
                       "Requests med CPR blokeres på inputniveau — teksten når aldrig AI-tjenesten."
        ),

        new(
            PiiType: "IBAN",
            Category: "Finansielle personoplysninger",
            Articles: ["Art. 4(1)", "Art. 5(1)(b)"],
            Principles: ["Formålsbegrænsning"],
            PolicyOutcome: "BLOCK",
            RiskLevel: RiskLevel.Critical,
            Rationale: "Bankkontonumre udgør finansielle personoplysninger med høj misbrug-risiko. " +
                       "Videresendelse til en AI-tjeneste har intet legitimt formål i dette system. " +
                       "Blokeres på inputniveau."
        ),

        new(
            PiiType: "CREDIT_CARD",
            Category: "Finansielle personoplysninger",
            Articles: ["Art. 4(1)", "Art. 5(1)(b)", "Art. 32"],
            Principles: ["Formålsbegrænsning", "Integritet og fortrolighed"],
            PolicyOutcome: "BLOCK",
            RiskLevel: RiskLevel.Critical,
            Rationale: "Kreditkortnumre er reguleret af både GDPR og PCI-DSS. " +
                       "Eksponering for AI-tjenester er uacceptabel sikkerhedsrisiko. " +
                       "Blokeres på inputniveau."
        ),

        new(
            PiiType: "PASSPORT",
            Category: "Særligt følsomme personoplysninger",
            Articles: ["Art. 4(1)", "Art. 87"],
            Principles: ["Dataminimering", "Formålsbegrænsning"],
            PolicyOutcome: "BLOCK",
            RiskLevel: RiskLevel.High,
            Rationale: "Pasnumre er officielle identifikationsdokumenter. " +
                       "Behandling kræver specifikt retsgrundlag — ikke tilgængeligt i dette POC. " +
                       "Blokeres på inputniveau."
        ),

        new(
            PiiType: "DK_ACCOUNT",
            Category: "Finansielle personoplysninger",
            Articles: ["Art. 4(1)", "Art. 5(1)(b)"],
            Principles: ["Formålsbegrænsning"],
            PolicyOutcome: "BLOCK",
            RiskLevel: RiskLevel.Critical,
            Rationale: "Danske bankregistrerings- og kontonumre kan bruges til direkte finansiel svindel. " +
                       "Blokeres på inputniveau — teksten når aldrig AI-tjenesten."
        ),

        new(
            PiiType: "IP_ADDRESS",
            Category: "Tekniske personoplysninger",
            Articles: ["Art. 4(1)"],
            Principles: ["Dataminimering"],
            PolicyOutcome: "ALLOW + SANITIZE",
            RiskLevel: RiskLevel.Low,
            Rationale: "EU-Domstolen (Breyer-sagen, C-582/14) fastslog at dynamiske IP-adresser " +
                       "kan være personoplysninger. Saniteres til <IP_ADDRESS_N> som forsigtighedsprincip."
        ),

        new(
            PiiType: "CVR",
            Category: "Virksomhedsidentifikator",
            Articles: ["Art. 4(1)"],
            Principles: ["Dataminimering"],
            PolicyOutcome: "ALLOW + SANITIZE",
            RiskLevel: RiskLevel.Low,
            Rationale: "CVR-numre identificerer virksomheder, ikke direkte fysiske personer. " +
                       "I enkeltmandsvirksomheder kan de dog indirekte identificere en person. Saniteres."
        ),

        new(
            PiiType: "URL",
            Category: "Tekniske personoplysninger",
            Articles: ["Art. 4(1)", "Art. 5(1)(c)"],
            Principles: ["Dataminimering"],
            PolicyOutcome: "ALLOW + SANITIZE",
            RiskLevel: RiskLevel.Low,
            Rationale: "URLs kan indeholde personoplysninger i query strings eller stier (fx bruger-ID). " +
                       "Saniteres som forsigtighedsprincip inden AI-kald."
        ),
    ];

    // ── Public API 

    public GdprMapResponse BuildMap()
    {
        var covered  = SystemControls.Count(c => c.Coverage == CoverageLevel.Covered);
        var partial  = SystemControls.Count(c => c.Coverage == CoverageLevel.Partial);
        var notCov   = SystemControls.Count(c => c.Coverage == CoverageLevel.NotCovered);

        var articles = SystemControls
            .SelectMany(c => c.Articles)
            .Concat(PiiClassifications.SelectMany(p => p.Articles))
            .Distinct()
            .OrderBy(a => a)
            .ToArray();

        var summary = new GdprCoverageSummary(
            TotalControls: SystemControls.Length,
            Covered: covered,
            Partial: partial,
            NotCovered: notCov,
            CoveredArticles: articles
        );

        return new GdprMapResponse(
            SystemControls: SystemControls,
            PiiClassifications: PiiClassifications,
            Summary: summary
        );
    }

    /// <summary>
    /// Returnerer de GDPR-klassifikationer der er relevante for et givet sæt findings.
    /// Bruges til at vise kontekstuel GDPR-info efter en analyse.
    /// </summary>
    public IReadOnlyList<GdprPiiClassification> GetRelevantClassifications(
        IEnumerable<string> piiTypes)
    {
        var set = new HashSet<string>(piiTypes, StringComparer.OrdinalIgnoreCase);
        return PiiClassifications
            .Where(p => set.Contains(p.PiiType))
            .ToList();
    }
}
