using System.Text.Json;
using ComplianceHub.Application.Features.Agent.Models;
using ComplianceHub.Application.Common.Interfaces;

namespace ComplianceHub.Infrastructure.Services.Ai.LlmClients;

internal sealed class AzureOpenAiLlmClient(
    IHttpClientFactory httpClientFactory,
    string apiKey,
    string endpoint,
    string deploymentName,
    string apiVersion) : LlmHttpBase(httpClientFactory)
{
    public override async Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct)
    {
        var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";

        var messages = BuildMessages(request);
        var body = BuildBody(request, messages);

        var client = CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", apiKey);
        req.Content = Json(body);

        using var resp = await SendWithRetryAsync(client, req, ct);
        var payload = await resp.Content.ReadAsStringAsync(ct);
        return ParseOpenAiResponse(payload);
    }

    private static object BuildMessages(LlmRequest request)
    {
        var list = new List<object>
        {
            new { role = "system", content = request.SystemPrompt }
        };
        foreach (var m in request.Messages)
            list.Add(BuildMessage(m));
        return list;
    }

    private static object BuildMessage(LlmMessage m) => m.Role == "tool"
        ? new { role = "tool", tool_call_id = m.ToolCallId ?? string.Empty, content = m.Content }
        : new { role = m.Role, content = m.Content };

    private static object BuildBody(LlmRequest request, object messages)
    {
        if (request.Tools is { Count: > 0 })
        {
            return new
            {
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

        return new
        {
            messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            response_format = new { type = "json_object" }
        };
    }

    internal static LlmResponse ParseOpenAiResponse(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var choice = doc.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");
        var stopReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() ?? "stop" : "stop";

        if (message.TryGetProperty("tool_calls", out var toolCallsEl) &&
            toolCallsEl.ValueKind == JsonValueKind.Array)
        {
            var toolCalls = toolCallsEl.EnumerateArray().Select(tc => new LlmToolCall(
                tc.GetProperty("id").GetString() ?? string.Empty,
                tc.GetProperty("function").GetProperty("name").GetString() ?? string.Empty,
                tc.GetProperty("function").GetProperty("arguments").GetString() ?? "{}"
            )).ToList();
            return new LlmResponse(null, toolCalls, "tool_use");
        }

        var content = message.TryGetProperty("content", out var c) ? c.GetString() : null;
        return new LlmResponse(content, null, stopReason);
    }
}
