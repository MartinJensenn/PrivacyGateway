using System.Text;
using UglyToad.PdfPig;

namespace PrivacyGateway.Api.Services;

public sealed class FileTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt", ".md", ".csv", ".log" };

    public string SupportedFormats =>
        string.Join(", ", SupportedExtensions.OrderBy(e => e));

    /// <summary>
    /// Extracts plain text from a file stream.
    /// Returns (text, null) on success or ("", errorMessage) on failure.
    /// </summary>
    public (string Text, string? Error) Extract(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty);

        if (string.IsNullOrEmpty(ext) || !SupportedExtensions.Contains(ext))
            return (string.Empty,
                $"Filtypen '{ext}' understøttes ikke. Understøttede typer: {SupportedFormats}");

        return ext.ToLowerInvariant() switch
        {
            ".pdf" => ExtractPdf(stream, fileName!),
            _      => ExtractPlainText(stream, fileName!)
        };
    }

    // ── PDF ─────────────────────────────────────────────────────────────────

    private static (string, string?) ExtractPdf(Stream stream, string fileName)
    {
        try
        {
            using var doc = PdfDocument.Open(stream);

            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);

            var text = sb.ToString().Trim();

            return string.IsNullOrWhiteSpace(text)
                ? (string.Empty,
                    $"'{fileName}' indeholder ingen læsbar tekst — PDF'en er muligvis scannet eller billed-baseret.")
                : (text, null);
        }
        catch (Exception ex)
        {
            return (string.Empty, $"Kunne ikke læse PDF '{fileName}': {ex.Message}");
        }
    }

    // ── Plain text (TXT, MD, CSV, LOG) ──────────────────────────────────────

    private static (string, string?) ExtractPlainText(Stream stream, string fileName)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return (reader.ReadToEnd(), null);
        }
        catch (Exception ex)
        {
            return (string.Empty, $"Kunne ikke læse '{fileName}': {ex.Message}");
        }
    }
}
