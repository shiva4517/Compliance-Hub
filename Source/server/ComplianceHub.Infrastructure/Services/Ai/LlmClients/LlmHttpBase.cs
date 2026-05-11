using System.Net;
using System.Text;
using System.Text.Json;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Agent.Models;

namespace ComplianceHub.Infrastructure.Services.Ai.LlmClients;

internal abstract class LlmHttpBase(IHttpClientFactory httpClientFactory) : ILlmClient
{
    protected static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Serializes with NO naming policy — property names written in code are sent as-is (snake_case preserved)
    private static readonly JsonSerializerOptions JsonExactOpts = new() { PropertyNamingPolicy = null };

    public abstract Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct);

    protected HttpClient CreateClient() => httpClientFactory.CreateClient("AIProviders");

    protected static StringContent Json(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");

    protected static StringContent JsonExact(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonExactOpts), Encoding.UTF8, "application/json");

    protected static async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient client, HttpRequestMessage request, CancellationToken ct)
    {
        const int maxAttempts = 3;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var cloned = await CloneAsync(request, ct);
            var response = await client.SendAsync(cloned, ct);

            if (response.IsSuccessStatusCode)
                return response;

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                response.Dispose();
                throw new InvalidOperationException($"LLM provider error ({(int)response.StatusCode}): {ExtractDetail(body)}");
            }

            if (attempt == maxAttempts)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var wait = response.Headers.RetryAfter?.Delta;
                response.Dispose();
                throw new InvalidOperationException(
                    wait is { } w
                        ? $"AI provider is rate-limiting. Retry after {Math.Ceiling(w.TotalSeconds)}s."
                        : "AI provider is rate-limiting. Please wait and try again.");
            }

            var retryWait = response.Headers.RetryAfter?.Delta ?? delay;
            response.Dispose();
            await Task.Delay(retryWait, ct);
            delay += delay;
        }

        throw new InvalidOperationException("Unable to complete LLM request.");
    }

    protected static string ExtractText(JsonDocument doc, params string[][] paths)
    {
        foreach (var path in paths)
        {
            try
            {
                JsonElement el = doc.RootElement;
                foreach (var segment in path)
                {
                    if (int.TryParse(segment, out var idx))
                        el = el[idx];
                    else
                        el = el.GetProperty(segment);
                }
                var text = el.GetString();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch { /* try next path */ }
        }
        throw new InvalidOperationException("LLM returned no usable content.");
    }

    protected static string ExtractJson(string raw)
    {
        var t = raw.Trim();
        if (t.StartsWith('{') && t.EndsWith('}')) return t;
        var s = t.IndexOf('{');
        var e = t.LastIndexOf('}');
        if (s >= 0 && e > s) return t[s..(e + 1)];
        throw new InvalidOperationException($"LLM response is not valid JSON: {t[..Math.Min(200, t.Length)]}");
    }

    private static string ExtractDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        try
        {
            using var d = JsonDocument.Parse(body);
            var r = d.RootElement;
            if (r.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.String) return err.GetString() ?? string.Empty;
                if (err.TryGetProperty("message", out var m)) return m.GetString() ?? string.Empty;
            }
            if (r.TryGetProperty("message", out var msg)) return msg.GetString() ?? string.Empty;
        }
        catch { }
        return body[..Math.Min(300, body.Length)];
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage src, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(src.Method, src.RequestUri);
        foreach (var h in src.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        if (src.Content is not null)
        {
            var bytes = await src.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var h in src.Content.Headers) clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        clone.Version = src.Version;
        clone.VersionPolicy = src.VersionPolicy;
        return clone;
    }
}
