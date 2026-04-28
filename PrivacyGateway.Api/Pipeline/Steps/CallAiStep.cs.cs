using System.ClientModel;
using OpenAI.Chat;
using PrivacyGateway.Api.Pipeline.Abstractions;
using PrivacyGateway.Api.Pipeline.Context;

namespace PrivacyGateway.Api.Pipeline.Steps;

public sealed class CallAiStep : IPipelineStep
{
    private readonly ChatClient? _client;

    public CallAiStep(ChatClient? client = null)
    {
        _client = client;
    }

    public string Name => "CallAi";
    public int Order => 60;
    public PipelineMode AppliesTo => PipelineMode.Chat;

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        if (context.IsTerminated) return;

        if (_client is null)
        {
            context.Fail(503, "OpenAI not configured. Set OPENAI_API_KEY (env var) or OpenAI:ApiKey in configuration.");
            return;
        }

        try
        {
            var userMessage = ChatMessage.CreateUserMessage(context.SanitizedText);
            var completion = await _client.CompleteChatAsync(new[] { userMessage }, cancellationToken: ct);
            context.AiResponse = completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            context.Fail(429, "OpenAI quota/rate limit hit (HTTP 429). Check billing/limits.");
        }
        catch (ClientResultException ex)
        {
            context.Fail(ex.Status, $"OpenAI call failed (HTTP {ex.Status}).");
        }
        catch (Exception ex)
        {
            context.Fail(500, $"Unexpected error calling AI: {ex.Message}");
        }
    }
}