namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class OutputGuardOptions
{
    // En liste over PII-typer som OutputGuard skal tjekke for i AI-svaret.
    public HashSet<string> BlockedTypes { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "EMAIL",
        "PHONE",
        "CPR",
        "IBAN",
        "CREDIT_CARD",
        "PASSPORT",
        "DK_ACCOUNT",
        "IP_ADDRESS",
        "CVR",
        "URL"
    };

    public bool BlockOnCpr { get; init; } = true;
    public bool BlockOnPassport { get; init; } = true;
    public bool BlockOnCreditCard { get; init; } = true;
    public bool BlockOnIban { get; init; } = true;
    public bool BlockOnDkAccount { get; init; } = true;

}