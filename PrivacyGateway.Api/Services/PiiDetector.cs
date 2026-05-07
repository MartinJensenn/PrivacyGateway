using System.Text.RegularExpressions;
using PrivacyGateway.Api.Models;

namespace PrivacyGateway.Api.Services;

public sealed class PiiDetector
{
    public const string Email = "EMAIL";
    public const string Phone = "PHONE";
    public const string Cpr = "CPR";
    public const string Iban = "IBAN";
    public const string CreditCard = "CREDIT_CARD";
    public const string Cvr = "CVR";
    public const string IpAddress = "IP_ADDRESS";
    public const string Passport = "PASSPORT";
    public const string DkAccount = "DK_ACCOUNT";
    public const string Url = "URL";

    private readonly (string Type, Regex Regex)[] _rules =
    [
        // Email (simpel, praktisk)
        (Email, new Regex(@"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // DK telefon: 8 cifre, evt. +45 og evt. mellemrum/bindestreger
        (Phone, new Regex(@"\b(?:\+45[\s\-]?)?(?:\d{2}[\s\-]?){3}\d{2}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // CPR (ddmmyyxxxx eller ddmmyy-xxxx) — køres FØR IBAN/CREDIT_CARD så overlap håndteres korrekt
        (Cpr, new Regex(@"\b\d{6}-?\d{4}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // Dansk IBAN: DK efterfulgt af 2 kontrolcifre + 14 cifre (evt. med mellemrum hver 4. tegn)
        (Iban, new Regex(@"\bDK\d{2}(?:\s?\d{4}){3}\s?\d{2}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // Kreditkortnummer: 16 cifre i grupper af 4 (Visa/MasterCard/Dankort-mønster)
        (CreditCard, new Regex(@"\b(?:\d{4}[\s\-]){3}\d{4}\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // CVR-nummer: præcis 8 cifre med "CVR"-præfix
        // Matcher "CVR: 12345678", "CVR-nr. 12345678", "CVR 12345678" osv.
        (Cvr, new Regex(@"\bCVR(?:[\s\-nr.:]{1,5})?\d{8}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // IPv4-adresse
        (IpAddress, new Regex(
            @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\b",
            RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // Dansk pasnummer: ét bogstav efterfulgt af præcis 8 cifre (fx A12345678)
        // Kræver bogstav-præfix for at undgå overlap med andre talsekvenser
        (Passport, new Regex(@"\b[A-Z]\d{8}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // Dansk bankkontonummer: registreringsnummer (4 cifre) + kontonummer (6-10 cifre)
        // Matcher "1234-1234567890", "reg. 1234 konto 123456789" og varianter
        (DkAccount, new Regex(@"\b(?:reg(?:istrerings)?(?:nr\.?|nummer|\.)?[\s\-]*)?(\d{4})[\s\-]+(?:konto(?:nr\.?|nummer)?[\s\-]*)?(\d{6,10})\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),

        // URL: http og https, inkl. stier og query strings
        // Matcher ikke bare "www." uden protokol for at holde false positives nede
        (Url, new Regex(@"https?://[^\s<>""']+",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),
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

        // Sorter fundne matches: først på startposition, så på længde (længere først for samme start)
        var ordered = candidates
            .OrderBy(f => f.Start)
            .ThenByDescending(f => f.Length)
            .ToList();

        // Fjern overlappende matches (simpelt: behold f�rste/bedste pr. overlap)
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

            // Hvis samme start som sidste og l�ngere, s� erstat
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