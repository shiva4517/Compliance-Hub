using System.Text.Json;
using ComplianceHub.Application.Features.Agent.Models;

namespace ComplianceHub.Infrastructure.Services.Ai.LlmClients;

internal sealed class GeminiLlmClient(
    IHttpClientFactory httpClientFactory,
    string apiKey,
    string model) : LlmHttpBase(httpClientFactory)
{
    public override async Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct)
    {
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? "gemini-2.0-flash" : model;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{resolvedModel}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        // Gemini uses a different message format — merge system prompt into first user turn
        var contents = new List<object>();
        foreach (var m in request.Messages)
        {
            var geminiRole = m.Role == "assistant" ? "model" : "user";
            contents.Add(new { role = geminiRole, parts = new[] { new { text = m.Content } } });
        }

        object body;
        if (request.Tools is { Count: > 0 })
        {
            body = new
            {
                system_instruction = new { parts = new[] { new { text = request.SystemPrompt } } },
                contents,
                tools = new[]
                {
                    new
                    {
                        function_declarations = request.Tools.Select(t => new
                        {
                            name = t.Name,
                            description = t.Description,
                            parameters = t.InputSchema
                        }).ToArray()
                    }
                },
                generationConfig = new { temperature = request.Temperature, maxOutputTokens = request.MaxTokens }
            };
        }
        else
        {
            body = new
            {
                system_instruction = new { parts = new[] { new { text = request.SystemPrompt } } },
                contents,
                generationConfig = new
                {
                    temperature = request.Temperature,
                    maxOutputTokens = request.MaxTokens,
                    responseMimeType = "application/json"
                }
            };
        }

        var client = CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = Json(body);

        using var resp = await SendWithRetryAsync(client, req, ct);
        var payload = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(payload);
        var candidate = doc.RootElement.GetProperty("candidates")[0];
        var part = candidate.GetProperty("content").GetProperty("parts")[0];

        if (part.TryGetProperty("functionCall", out var fc))
        {
            var toolCallId = Guid.NewGuid().ToString("N")[..8];
            var argsJson = fc.TryGetProperty("args", out var args)
                ? JsonSerializer.Serialize(args)
                : "{}";
            return new LlmResponse(null,
                [new LlmToolCall(toolCallId, fc.GetProperty("name").GetString() ?? string.Empty, argsJson)],
                "tool_use");
        }

        var text = part.TryGetProperty("text", out var t) ? t.GetString() : null;
        return new LlmResponse(text, null, "stop");
    }
}
