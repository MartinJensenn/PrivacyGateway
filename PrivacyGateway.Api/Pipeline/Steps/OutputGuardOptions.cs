namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class OutputGuardOptions
{
    // Hvilke PII-typer skal OutputGuard reagere på i AI-svar?
    public HashSet<string> BlockedTypes { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "EMAIL",
        "PHONE",
        "CPR"
    };

    // Skal hele svaret blokeres hvis CPR findes, eller bare saniteres?
    public bool BlockOnCpr { get; init; } = true;
}