using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.Client;

public sealed class PrivacyGatewayClient
{
    private readonly IHttpClientFactory _factory;

    public PrivacyGatewayClient(IHttpClientFactory factory) => _factory = factory;

    // ── Text endpoints ───────────────────────────────────────────────────────

    public async Task<AnalyzeResponse> AnalyzeAsync(string text)
    {
        var http = _factory.CreateClient("GatewayApi");
        var resp = await http.PostAsJsonAsync("/analyze", new AnalyzeRequest(text));

        if (!resp.IsSuccessStatusCode)
            throw new Exception(await resp.Content.ReadAsStringAsync());

        return (await resp.Content.ReadFromJsonAsync<AnalyzeResponse>())!;
    }

    public async Task<ChatResponse> ChatAsync(string text)
    {
        var http = _factory.CreateClient("GatewayApi");
        var resp = await http.PostAsJsonAsync("/chat", new ChatRequest(text));

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(
                await resp.Content.ReadAsStringAsync(),
                inner: null,
                statusCode: resp.StatusCode);

        return (await resp.Content.ReadFromJsonAsync<ChatResponse>())!;
    }

    // ── File endpoint ────────────────────────────────────────────────────────

    /// <summary>
    /// Uploader én eller flere filer til /analyze/files.
    /// Hvert fil analyseres separat for PII og returnerer et FileAnalyzeResult.
    /// Maks filstørrelse: 10 MB pr. fil.
    /// </summary>
    public async Task<IReadOnlyList<FileAnalyzeResult>> AnalyzeFilesAsync(
        IReadOnlyList<IBrowserFile> files,
        CancellationToken ct = default)
    {
        var http = _factory.CreateClient("GatewayApi");

        using var form    = new MultipartFormDataContent();
        var       streams = new List<Stream>(files.Count);

        try
        {
            foreach (var file in files)
            {
                const long maxSize = 10L * 1024 * 1024; // 10 MB
                var stream = file.OpenReadStream(maxAllowedSize: maxSize, cancellationToken: ct);
                streams.Add(stream);

                var content = new StreamContent(stream);
                content.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType);

                form.Add(content, "files", file.Name);
            }

            var resp = await http.PostAsync("/analyze/files", form, ct);

            if (!resp.IsSuccessStatusCode)
                throw new Exception(await resp.Content.ReadAsStringAsync(ct));

            return (await resp.Content.ReadFromJsonAsync<IReadOnlyList<FileAnalyzeResult>>(
                cancellationToken: ct))!;
        }
        finally
        {
            foreach (var s in streams)
                await s.DisposeAsync();
        }
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────
// Holdes identiske med API-modellerne (samme property navne).

public sealed record AnalyzeRequest(string Text);
public sealed record ChatRequest(string Text);

public sealed record Finding(string Type, int Start, int Length);

public sealed record AnalyzeResponse(
    string Decision,
    string Reason,
    string SanitizedText,
    IReadOnlyList<Finding> Findings
);

public sealed record ChatResponse(
    string Decision,
    string Reason,
    string SanitizedText,
    IReadOnlyList<Finding> Findings,
    string? AiResponse
);

public sealed record FileAnalyzeResult(
    string FileName,
    long FileSizeBytes,
    string? ExtractError,
    string Decision,
    string Reason,
    string SanitizedText,
    IReadOnlyList<Finding> Findings
);
