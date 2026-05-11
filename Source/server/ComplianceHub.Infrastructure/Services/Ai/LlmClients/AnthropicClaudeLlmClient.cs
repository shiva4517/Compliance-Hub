using System.Text.Json;
using ComplianceHub.Application.Features.Agent.Models;

namespace ComplianceHub.Infrastructure.Services.Ai.LlmClients;

internal sealed class AnthropicClaudeLlmClient(
    IHttpClientFactory httpClientFactory,
    string apiKey,
    string model) : LlmHttpBase(httpClientFactory)
{
    private const string AnthropicVersion = "2023-06-01";

    public override async Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct)
    {
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? "claude-sonnet-4-6" : model;

        var messages = request.Messages.Select(m =>
        {
            // Tool result: wrap in user message with tool_result block
            if (m.Role == "tool")
                return (object)new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "tool_result", tool_use_id = m.ToolCallId ?? string.Empty, content = m.Content }
                    }
                };

            // Assistant tool-call: reconstruct tool_use block Anthropic needs to see in history
            if (m.Role == "assistant" && m.ToolCallId is not null)
            {
                object inputObj;
                try { inputObj = JsonSerializer.Deserialize<object>(m.Content) ?? new { }; }
                catch { inputObj = new { }; }

                return (object)new
                {
                    role = "assistant",
                    content = new object[]
                    {
                        new { type = "tool_use", id = m.ToolCallId, name = m.ToolName ?? string.Empty, input = inputObj }
                    }
                };
            }

            return new { role = m.Role, content = (object)m.Content };
        }).ToArray();

        object body;
        if (request.Tools is { Count: > 0 })
        {
            body = new
            {
                model = resolvedModel,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                system = request.SystemPrompt,
                messages,
                tools = request.Tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    input_schema = t.InputSchema
                }).ToArray()
            };
        }
        else
        {
            body = new
            {
                model = resolvedModel,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                system = request.SystemPrompt + "\n\nIMPORTANT: Return JSON only.",
                messages
            };
        }

        var client = CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", AnthropicVersion);
        req.Content = JsonExact(body);

        using var resp = await SendWithRetryAsync(client, req, ct);
        var payload = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(payload);
        var stopReason = doc.RootElement.TryGetProperty("stop_reason", out var sr) ? sr.GetString() ?? "end_turn" : "end_turn";
        var contentArr = doc.RootElement.GetProperty("content");

        var toolCalls = new List<LlmToolCall>();
        string? textContent = null;

        foreach (var block in contentArr.EnumerateArray())
        {
            var type = block.TryGetProperty("type", out var t) ? t.GetString() : null;
            if (type == "tool_use")
            {
                var id = block.GetProperty("id").GetString() ?? string.Empty;
                var name = block.GetProperty("name").GetString() ?? string.Empty;
                var inputJson = block.TryGetProperty("input", out var inp)
                    ? JsonSerializer.Serialize(inp)
                    : "{}";
                toolCalls.Add(new LlmToolCall(id, name, inputJson));
            }
            else if (type == "text")
            {
                textContent = block.TryGetProperty("text", out var tx) ? tx.GetString() : null;
            }
        }

        if (toolCalls.Count > 0)
            return new LlmResponse(null, toolCalls, "tool_use");

        return new LlmResponse(textContent, null, stopReason);
    }
}
