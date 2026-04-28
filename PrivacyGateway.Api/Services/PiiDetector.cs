using System.Text.RegularExpressions;
using PrivacyGateway.Api.Models;

namespace PrivacyGateway.Api.Services;

public sealed class PiiDetector
{
    public const string Email = "EMAIL";
    public const string Phone = "PHONE";
    public const string Cpr = "CPR";

    private readonly (string Type, Regex Regex)[] _rules =
    [
        // Email (simpel, praktisk)
        (Email, new Regex(@"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // DK telefon (meget simpel): 8 cifre, evt. +45 og evt. mellemrum
        (Phone, new Regex(@"\b(?:\+45\s*)?(?:\d{2}\s?\d{2}\s?\d{2}\s?\d{2}|\d{8})\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // CPR (ddmmyyxxxx eller ddmmyy-xxxx)
        (Cpr, new Regex(@"\b\d{6}-?\d{4}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled)),
    ];

    public IReadOnlyList<Finding> FindAll(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<Finding>();

        var candidates = new List<Finding>(capacity: 16);

        foreach (var rule in _rules)
        {
            foreach (Match m in rule.Regex.Matches(text))
            {
                if (!m.Success) continue;
                candidates.Add(new Finding(rule.Type, m.Index, m.Length));
            }
        }

        if (candidates.Count == 0)
            return Array.Empty<Finding>();

        // Sortér venstre->højre, og foretræk længere match ved samme start
        var ordered = candidates
            .OrderBy(f => f.Start)
            .ThenByDescending(f => f.Length)
            .ToList();

        // Fjern overlappende matches (simpelt: behold første/bedste pr. overlap)
        var result = new List<Finding>(ordered.Count);
        int lastEnd = -1;

        foreach (var f in ordered)
        {
            if (result.Count == 0 || f.Start >= lastEnd)
            {
                result.Add(f);
                lastEnd = f.Start + f.Length;
                continue;
            }

            // Hvis samme start som sidste og længere, så erstat
            var last = result[^1];
            if (f.Start == last.Start && f.Length > last.Length)
            {
                result[^1] = f;
                lastEnd = f.Start + f.Length;
            }
        }

        return result;
    }
}