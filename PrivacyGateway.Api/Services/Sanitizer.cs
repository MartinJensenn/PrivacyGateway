using System.Text;
using PrivacyGateway.Api.Models;

namespace PrivacyGateway.Api.Services;

public sealed class Sanitizer
{
    public string Sanitize(string? text, IReadOnlyList<Finding> findings)
    {
        if (string.IsNullOrEmpty(text) || findings.Count == 0)
            return text ?? string.Empty;

        // Opret tokens for hver fundet PII-segment, og hold styr på tællere for hver type
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tokenized = findings
            .OrderBy(f => f.Start)
            .Select(f =>
            {
                counters.TryGetValue(f.Type, out int n);
                n++;
                counters[f.Type] = n;
                string token = $"<{f.Type}_{n}>";
                return (Finding: f, Token: token);
            })
            .ToList();

        // Erstat fundne PII-segmenter med tokens i teksten
        var sb = new StringBuilder(text);

        foreach (var item in tokenized.OrderByDescending(x => x.Finding.Start))
        {
            sb.Remove(item.Finding.Start, item.Finding.Length);
            sb.Insert(item.Finding.Start, item.Token);
        }

        return sb.ToString();
    }
}