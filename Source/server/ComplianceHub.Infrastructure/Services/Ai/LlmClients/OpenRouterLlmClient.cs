using System.Net.Http.Headers;
using ComplianceHub.Application.Features.Agent.Models;

namespace ComplianceHub.Infrastructure.Services.Ai.LlmClients;

internal sealed class OpenRouterLlmClient(
    IHttpClientFactory httpClientFactory,
    string apiKey,
    string model) : LlmHttpBase(httpClientFactory)
{
    public override async Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct)
    {
        var messages = new List<object>
        {
            new { role = "system", content = request.SystemPrompt }
        };
        foreach (var m in request.Messages)
            messages.Add(m.Role == "tool"
                ? new { role = "tool", tool_call_id = m.ToolCallId ?? string.Empty, content = m.Content }
                : (object)new { role = m.Role, content = m.Content });

        object body;
        if (request.Tools is { Count: > 0 })
        {
            body = new
            {
                model,
                messages,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                tools = request.Tools.Select(t => new
                {
                    type = "function",
                    function = new { name = t.Name, description = t.Description, parameters = t.InputSchema }
                }).ToArray(),
                tool_choice = "auto"
            };
        }
        else
        {
            body = new
            {
                model,
                messages,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                response_format = new { type = "json_object" }
            };
        }

        var client = CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = Json(body);

        using var resp = await SendWithRetryAsync(client, req, ct);
        var payload = await resp.Content.ReadAsStringAsync(ct);
        return AzureOpenAiLlmClient.ParseOpenAiResponse(payload);
    }
}
