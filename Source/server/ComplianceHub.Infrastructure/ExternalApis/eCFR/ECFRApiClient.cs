using System.Net;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polly;
using Polly.Retry;

namespace ComplianceHub.Infrastructure.ExternalApis.eCFR;

public class ECFRApiClient(IHttpClientFactory httpClientFactory, ILogger<ECFRApiClient> logger) : IECFRApiClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        MaxDepth = 1280, // Extra padding for deep federal structures
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles // Useful if the DTOs are complex
    };

    // Retryable HTTP status codes: rate-limited or server-side transient errors
    private static readonly HashSet<HttpStatusCode> _retryableStatuses =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.BadGateway,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.InternalServerError,
    ];

    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 4,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => _retryableStatuses.Contains(r.StatusCode)),
                OnRetry = static args =>
                {
                    var status = args.Outcome.Result?.StatusCode.ToString() ?? args.Outcome.Exception?.GetType().Name;
                    args.Context.Properties.TryGetValue(new ResiliencePropertyKey<ILogger>("logger"), out var log);
                    log?.LogWarning("eCFR retry {Attempt}/{Max} after {Delay:s\\.ff}s — {Reason}",
                        args.AttemptNumber + 1, 4, args.RetryDelay, status);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

    private HttpClient Client => httpClientFactory.CreateClient("eCFR");

    private Task<HttpResponseMessage> GetWithRetryAsync(string url, CancellationToken ct) =>
        GetWithRetryAsync(url, HttpCompletionOption.ResponseContentRead, ct);

    private Task<HttpResponseMessage> GetWithRetryAsync(string url, HttpCompletionOption completion, CancellationToken ct)
    {
        var context = ResilienceContextPool.Shared.Get(ct);
        context.Properties.Set(new ResiliencePropertyKey<ILogger>("logger"), logger);
        return _retryPipeline.ExecuteAsync(async ctx =>
            await Client.GetAsync(url, completion, ctx.CancellationToken), context)
            .AsTask()
            .ContinueWith(t => { ResilienceContextPool.Shared.Return(context); return t.Result; }, TaskScheduler.Default);
    }

    public async Task<IReadOnlyList<EcfrTitleDto>> GetTitlesAsync(CancellationToken ct = default)
    {
        var response = await GetWithRetryAsync("/api/versioner/v1/titles.json", ct);
        await EnsureSuccessAsync(response, "fetch titles list", ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<EcfrTitlesResponse>(content, _jsonOptions);
        return result?.Titles ?? [];
    }

    public async Task<EcfrStructureNodeDto> GetTitleStructureAsync(int titleNumber, DateOnly date, CancellationToken ct = default)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        using var response = await GetWithRetryAsync($"/api/versioner/v1/structure/{dateStr}/title-{titleNumber}.json", HttpCompletionOption.ResponseHeadersRead, ct);
        logger.LogInformation("eCFR GET {Url} returned {Status}", $"/api/versioner/v1/structure/{dateStr}/title-{titleNumber}.json", response.StatusCode);
        var contentLength = response.Content.Headers.ContentLength;
        logger.LogInformation("eCFR content length: {Length}", contentLength?.ToString() ?? "null");
        await EnsureSuccessAsync(response, $"fetch structure for Title {titleNumber}", ct);

        // Save response to a temp file so we can inspect it and avoid issues when
        // deserializing directly from a forward-only stream for debugging.
        var tempFile = Path.Combine(Path.GetTempPath(), $"ecfr-structure-title-{titleNumber}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.json");
        await using (var responseStream = await response.Content.ReadAsStreamAsync(ct))
        await using (var fs = File.Create(tempFile))
        {
            await responseStream.CopyToAsync(fs, ct);
        }
        logger.LogInformation("Wrote eCFR response to {Path}", tempFile);

        // Inspect the JSON root to understand why deserialization might return null
        try
        {
            await using var inspectFs = File.OpenRead(tempFile);
            using var doc = await JsonDocument.ParseAsync(inspectFs, new JsonDocumentOptions { MaxDepth = 1280 }, ct);
            var root = doc.RootElement;
            logger.LogInformation("eCFR root element kind: {Kind}", root.ValueKind);
            if (root.ValueKind == JsonValueKind.Object)
            {
                var props = root.EnumerateObject().Select(p => p.Name).ToArray();
                logger.LogInformation("eCFR top-level properties: {Props}", string.Join(", ", props));
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                logger.LogInformation("eCFR root is an array with {Count} elements", root.GetArrayLength());
            }
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "Failed to parse eCFR JSON for title {Title}", titleNumber);
        }

        // Attempt deserialization from the saved file. If it returns null, try a fallback
        // of deserializing an array of responses (some endpoints sometimes return arrays).
        await using (var fsForDeserialize = File.OpenRead(tempFile))
        {
            try
            {
                var result = await JsonSerializer.DeserializeAsync<EcfrStructureNodeDto>(fsForDeserialize, _jsonOptions, ct);
                if (result != null)
                {
                    return result;
                }
            }
            catch (JsonException jex)
            {
                logger.LogError(jex, "Primary deserialization failed for title {Title}", titleNumber);
            }
        }

        // Fallback: try deserializing an array of EcfrStructureResponse and use first element
        try
        {
            await using var fsFallback = File.OpenRead(tempFile);
            var arr = await JsonSerializer.DeserializeAsync<EcfrStructureResponse[]>(fsFallback, _jsonOptions, ct);
            if (arr != null && arr.Length > 0)
            {
                logger.LogInformation("Deserialized eCFR response as array with {Count} entries, using first.", arr.Length);
                return arr[0].Structure ?? new EcfrStructureNodeDto();
            }
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "Fallback array deserialization failed for title {Title}", titleNumber);
        }

        logger.LogWarning("Unable to deserialize eCFR structure for title {Title}; returning empty structure.", titleNumber);
        return new EcfrStructureNodeDto();
    }

    public async Task<EcfrVersionsResponse> GetVersionsSinceAsync(int titleNumber, DateOnly sinceDate, CancellationToken ct = default)
    {
        var dateStr = sinceDate.ToString("yyyy-MM-dd");
        var response = await GetWithRetryAsync(
            $"/api/versioner/v1/versions/title-{titleNumber}.json?issue_date[gte]={dateStr}", ct);
        await EnsureSuccessAsync(response, $"fetch versions for Title {titleNumber} since {dateStr}", ct);
        var content = await response.Content.ReadAsStringAsync(ct);       
        var result = JsonSerializer.Deserialize<EcfrVersionsResponse>(content, _jsonOptions);
        return result!;
    }

    public async Task<string> GetSectionContentHtmlAsync(DateOnly date, string sectionPath, CancellationToken ct = default)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var response = await GetWithRetryAsync($"/api/renderer/v1/content/enhanced/{dateStr}/{sectionPath}", ct);
        await EnsureSuccessAsync(response, $"fetch section HTML at {sectionPath}", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetCurrentSectionContentHtmlAsync(int titleNumber, int partNumber, string sectionIdentifier, CancellationToken ct = default)
    {
        var url = $"/api/renderer/v1/content/enhanced/current/title-{titleNumber}?part={partNumber}&section={sectionIdentifier}";
        var response = await GetWithRetryAsync(url, ct);
        await EnsureSuccessAsync(response, $"fetch current HTML for section {sectionIdentifier} (Title {titleNumber}, Part {partNumber})", ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = string.Empty;
        try { body = await response.Content.ReadAsStringAsync(ct); } catch { /* best-effort */ }

        var detail = response.StatusCode switch
        {
            HttpStatusCode.Forbidden       => $"eCFR denied access while trying to {operation}. The request was forbidden (403). Check API credentials or rate limits.",
            HttpStatusCode.Unauthorized    => $"eCFR rejected the request while trying to {operation}. Authentication is required (401).",
            HttpStatusCode.NotFound        => $"eCFR could not find the requested resource while trying to {operation} (404).",
            HttpStatusCode.TooManyRequests => $"eCFR rate limit exceeded while trying to {operation} (429). Please retry later.",
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout
                                           => $"eCFR is temporarily unavailable while trying to {operation} ({(int)response.StatusCode}). Please retry later.",
            _                              => $"eCFR returned an unexpected error while trying to {operation}: {(int)response.StatusCode} {response.ReasonPhrase}."
        };

        logger.LogError("eCFR API error — {Detail} | Response body: {Body}", detail, body);
        throw new EcfrApiException(detail, response.StatusCode, body);
    }
}

public sealed class EcfrApiException(string message, HttpStatusCode statusCode, string responseBody)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}
