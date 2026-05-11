using System.Net;
using System.Text.Json;
using ComplianceHub.Functions.Models;

namespace ComplianceHub.Functions.Services;

public class EcfrClient(IHttpClientFactory factory)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http = factory.CreateClient("eCFR");

    public async Task<List<EcfrTitle>> GetTitlesAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/api/versioner/v1/titles.json", ct);
        await EnsureSuccessAsync(response, "fetch titles");
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TitlesResponse>(json, _json)?.Titles ?? [];
    }

    public async Task<EcfrNode> GetStructureAsync(int titleNumber, DateOnly date, CancellationToken ct)
    {
        var url = $"/api/versioner/v1/structure/{date:yyyy-MM-dd}/title-{titleNumber}.json";
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, $"fetch structure for Title {titleNumber}");
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<EcfrNode>(json, _json)!;
    }

    public async Task<List<EcfrVersionEntry>> GetVersionsSinceAsync(int titleNumber, DateOnly since, CancellationToken ct)
    {
        var url = $"/api/versioner/v1/versions/title-{titleNumber}.json?issue_date[gte]={since:yyyy-MM-dd}";
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, $"fetch versions for Title {titleNumber} since {since:yyyy-MM-dd}");
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<VersionsResponse>(json, _json)?.ContentVersions ?? [];
    }

    public async Task<string> GetSectionHtmlAsync(DateOnly date, string path, CancellationToken ct)
    {
        var url = $"/api/renderer/v1/content/enhanced/{date:yyyy-MM-dd}/{path}";
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, $"fetch section HTML at {path}");
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetCurrentSectionHtmlAsync(DateOnly date, EcfrVersionEntry ecfrVersionEntry, CancellationToken ct)
    {
        var url = $"/api/renderer/v1/content/enhanced/current/title-{ecfrVersionEntry.Title}?part={ecfrVersionEntry.Part}&section={ecfrVersionEntry.Identifier}";
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response, $"fetch current HTML for section {ecfrVersionEntry.Identifier} (Title {ecfrVersionEntry.Title}, Part {ecfrVersionEntry.Part})");
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;

        var body = string.Empty;
        try { body = await response.Content.ReadAsStringAsync(); } catch { /* best-effort */ }

        var detail = response.StatusCode switch
        {
            HttpStatusCode.Forbidden    => $"eCFR denied access while trying to {operation}. The request was forbidden (403). Check API credentials or rate limits.",
            HttpStatusCode.Unauthorized => $"eCFR rejected the request while trying to {operation}. Authentication is required (401).",
            HttpStatusCode.NotFound     => $"eCFR could not find the requested resource while trying to {operation} (404).",
            HttpStatusCode.TooManyRequests => $"eCFR rate limit exceeded while trying to {operation} (429). Please retry later.",
            HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout
                => $"eCFR is temporarily unavailable while trying to {operation} ({(int)response.StatusCode}). Please retry later.",
            _ => $"eCFR returned an unexpected error while trying to {operation}: {(int)response.StatusCode} {response.ReasonPhrase}."
        };

        throw new EcfrApiException(detail, response.StatusCode, body);
    }
}

public sealed class EcfrApiException(string message, HttpStatusCode statusCode, string responseBody)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}
