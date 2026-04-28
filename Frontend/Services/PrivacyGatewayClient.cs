using System.Net.Http.Json;

namespace Frontend.Client;

public sealed class PrivacyGatewayClient
{
    private readonly IHttpClientFactory _factory;

    public PrivacyGatewayClient(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<AnalyzeResponse> AnalyzeAsync(string text)
    {
        var http = _factory.CreateClient("GatewayApi");
        var resp = await http.PostAsJsonAsync("/analyze", new AnalyzeRequest(text));

        // hvis API returnerer ProblemDetails
        if (!resp.IsSuccessStatusCode)
            throw new Exception(await resp.Content.ReadAsStringAsync());

        return (await resp.Content.ReadFromJsonAsync<AnalyzeResponse>())!;
    }

    public async Task<ChatResponse> ChatAsync(string text)
    {
        var http = _factory.CreateClient("GatewayApi");
        var resp = await http.PostAsJsonAsync("/chat", new ChatRequest(text));

        if (!resp.IsSuccessStatusCode)
            throw new Exception(await resp.Content.ReadAsStringAsync());

        return (await resp.Content.ReadFromJsonAsync<ChatResponse>())!;
    }
}

// --- DTO (Data Transfer Object) modeller ---
// Hold dem identiske med din API (samme property navne).
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