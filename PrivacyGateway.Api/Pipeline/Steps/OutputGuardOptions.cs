namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class OutputGuardOptions
{
    // Hvilke PII-typer skal OutputGuard reagere på i AI-svar?
    public HashSet<string> BlockedTypes { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "EMAIL",
        "PHONE",
        "CPR",
        "IBAN",
        "CreditCard",
        "Passport",
        "DkAccount",
        "IpAddress",
        "CVR",
        "URL"
    };

    // Skal hele svaret blokeres hvis X findes, eller bare saniteres?
    public bool BlockOnCpr { get; init; } = true;
    public bool BlockOnPassport { get; init; } = true;
    public bool BlockOnCreditCard { get; init; } = true;
    public bool BlockOnIban { get; init; } = true;
    public bool BlockOnDkAccount { get; init; } = true;

}