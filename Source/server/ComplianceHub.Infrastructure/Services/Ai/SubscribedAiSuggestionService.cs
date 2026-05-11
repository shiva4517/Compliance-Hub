using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ComplianceHub.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Infrastructure.Services.Ai;

public class SubscribedAiSuggestionService(
    IHttpClientFactory httpClientFactory,
    ILogger<SubscribedAiSuggestionService> logger) : IAiSuggestionService
{
    private static readonly string[] FrequencyTypes =
    [
        "Daily",
        "Weekly",
        "Bi-Weekly",
        "Monthly",
        "Quarterly",
        "Semi-Annually",
        "Annually",
        "One-Time"
    ];

    private static readonly string[] DueDateTypes =
    [
        "Day of Month",
        "Day of Week",
        "Fixed Date",
        "Relative to Start",
        "End of Month",
        "Immediate",
        "Milestone Based"
    ];

    public async Task<RegulationSuggestionResult> SuggestRegulationDetailAsync(
        RegulationSuggestionContext context,
        AiProviderConnectionSettings connection,
        CancellationToken ct)
    {
        var prompt = BuildPrompt(context);

        logger.LogInformation(
            "AI suggest request: provider={Provider} model={Model} level={Level} node='{Node}' section='{Section}' hasContent={HasContent} contentChars={Chars} promptChars={PromptChars}",
            connection.Provider,
            connection.Model ?? "(default)",
            context.SubscribingLevel ?? "(legacy)",
            context.SubscribedNodeName ?? "(none)",
            context.SectionNumber,
            !string.IsNullOrWhiteSpace(context.HtmlContent),
            context.HtmlContent?.Length ?? 0,
            prompt.Length);

        var rawJson = connection.Provider switch
        {
            "azure-openai" => await CallAzureOpenAiAsync(connection, prompt, ct),
            "gemini" => await CallGeminiAsync(connection, prompt, ct),
            "claude" => await CallClaudeAsync(connection, prompt, ct),
            "openai" => await CallOpenAiAsync(connection, prompt, ct),
            "openrouter" => await CallOpenRouterAsync(connection, prompt, ct),
            _ => throw new InvalidOperationException("Unsupported AI provider.")
        };

        logger.LogInformation("AI suggest raw response ({Provider}, {Chars} chars): {Raw}",
            connection.Provider, rawJson?.Length ?? 0, rawJson);

        var result = ParseSuggestionResult(rawJson ?? "{}", connection.Provider);

        logger.LogInformation(
            "AI suggest parsed: freq={Freq} due={Due} min={Min} max={Max}",
            result.FrequencyType, result.DueDateType, result.MinValue, result.MaxValue);

        return result;
    }

    public async Task<IReadOnlyList<AiProviderModelOption>> GetAvailableModelsAsync(
        AiProviderConnectionSettings connection,
        CancellationToken ct)
    {
        return connection.Provider switch
        {
            "azure-openai" => await GetAzureOpenAiModelsAsync(connection, ct),
            "gemini" => await GetGeminiModelsAsync(connection, ct),
            "claude" => await GetClaudeModelsAsync(connection, ct),
            "openai" => await GetOpenAiModelsAsync(connection, ct),
            "openrouter" => await GetOpenRouterModelsAsync(connection, ct),
            _ => throw new InvalidOperationException("Unsupported AI provider.")
        };
    }

    private async Task<string> CallAzureOpenAiAsync(AiProviderConnectionSettings connection, string prompt, CancellationToken ct)
    {
        var deploymentName = string.IsNullOrWhiteSpace(connection.DeploymentName)
            ? connection.Model
            : connection.DeploymentName;

        if (string.IsNullOrWhiteSpace(connection.Endpoint) || string.IsNullOrWhiteSpace(deploymentName))
            throw new InvalidOperationException("Azure OpenAI endpoint and model are required.");

        var client = httpClientFactory.CreateClient();
        var endpoint = connection.Endpoint.TrimEnd('/');
        var apiVersion = connection.ApiVersion ?? "2024-02-15-preview";
        var url = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("api-key", connection.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            messages = new object[]
            {
                new { role = "system", content = "You are a regulatory compliance assistant. Return strict JSON only." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            response_format = new { type = "json_object" }
        }), Encoding.UTF8, "application/json");

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? throw new InvalidOperationException("Azure OpenAI returned no content.");
    }

    private async Task<string> CallGeminiAsync(AiProviderConnectionSettings connection, string prompt, CancellationToken ct)
    {
        var model = connection.Model ?? "gemini-2.0-flash";
        var client = httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(connection.ApiKey)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        }), Encoding.UTF8, "application/json");

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        return document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? throw new InvalidOperationException("Gemini returned no content.");
    }

    private async Task<string> CallClaudeAsync(AiProviderConnectionSettings connection, string prompt, CancellationToken ct)
    {
        var model = connection.Model ?? "claude-3-5-sonnet-latest";
        var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", connection.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model,
            max_tokens = 800,
            temperature = 0.2,
            system = "You are a regulatory compliance assistant. Return strict JSON only.",
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt }
                    }
                }
            }
        }), Encoding.UTF8, "application/json");

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        return document.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? throw new InvalidOperationException("Claude returned no content.");
    }

    private async Task<string> CallOpenAiAsync(AiProviderConnectionSettings connection, string prompt, CancellationToken ct)
    {
        var model = connection.Model ?? "gpt-5.4-mini";
        var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new { type = "input_text", text = "You are a regulatory compliance assistant. Return strict JSON only." }
                    }
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = prompt }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_object"
                }
            }
        }), Encoding.UTF8, "application/json");

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);

        if (document.RootElement.TryGetProperty("output_text", out var outputText))
        {
            var text = outputText.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        var content = document.RootElement
            .GetProperty("output")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return content ?? throw new InvalidOperationException("OpenAI returned no content.");
    }

    private async Task<string> CallOpenRouterAsync(AiProviderConnectionSettings connection, string prompt, CancellationToken ct)
    {
        var model = connection.Model ?? "openai/gpt-4.1-mini";
        var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = "You are a regulatory compliance assistant. Return strict JSON only." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            response_format = new { type = "json_object" }
        }), Encoding.UTF8, "application/json");

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? throw new InvalidOperationException("OpenRouter returned no content.");
    }

    private async Task<IReadOnlyList<AiProviderModelOption>> GetAzureOpenAiModelsAsync(
        AiProviderConnectionSettings connection,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(connection.Endpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is required.");

        var client = httpClientFactory.CreateClient();
        var endpoint = connection.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/models?api-version=2024-10-21";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("api-key", connection.ApiKey);

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        var items = document.RootElement.GetProperty("data")
            .EnumerateArray()
            .Where(item =>
                item.TryGetProperty("capabilities", out var capabilities) &&
                capabilities.TryGetProperty("chat_completion", out var chatCompletion) &&
                chatCompletion.ValueKind is JsonValueKind.True or JsonValueKind.False &&
                chatCompletion.GetBoolean())
            .Select(item =>
            {
                var id = item.GetProperty("id").GetString() ?? string.Empty;
                return new AiProviderModelOption(id, id);
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .DistinctBy(item => item.Value)
            .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (items.Length == 0)
            throw new InvalidOperationException("No Azure OpenAI chat models were returned for this endpoint.");

        return items;
    }

    private async Task<IReadOnlyList<AiProviderModelOption>> GetGeminiModelsAsync(
        AiProviderConnectionSettings connection,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(connection.ApiKey)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        var items = document.RootElement.GetProperty("models")
            .EnumerateArray()
            .Where(item =>
                item.TryGetProperty("supportedGenerationMethods", out var methods) &&
                methods.EnumerateArray().Any(method => string.Equals(method.GetString(), "generateContent", StringComparison.OrdinalIgnoreCase)))
            .Select(item =>
            {
                var name = item.GetProperty("name").GetString() ?? string.Empty;
                var value = name.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? name["models/".Length..] : name;
                var label = item.TryGetProperty("displayName", out var displayName)
                    ? displayName.GetString() ?? value
                    : value;
                return new AiProviderModelOption(value, label);
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .DistinctBy(item => item.Value)
            .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (items.Length == 0)
            throw new InvalidOperationException("No Gemini generation models were returned for this API key.");

        return items;
    }

    private async Task<IReadOnlyList<AiProviderModelOption>> GetClaudeModelsAsync(
        AiProviderConnectionSettings connection,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
        request.Headers.Add("x-api-key", connection.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        var items = document.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item =>
            {
                var id = item.GetProperty("id").GetString() ?? string.Empty;
                var label = item.TryGetProperty("display_name", out var displayName)
                    ? displayName.GetString() ?? id
                    : id;
                return new AiProviderModelOption(id, label);
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .DistinctBy(item => item.Value)
            .OrderByDescending(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (items.Length == 0)
            throw new InvalidOperationException("No Claude models were returned for this API key.");

        return items;
    }

    private async Task<IReadOnlyList<AiProviderModelOption>> GetOpenAiModelsAsync(
        AiProviderConnectionSettings connection,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        var items = document.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item =>
            {
                var id = item.GetProperty("id").GetString() ?? string.Empty;
                return new AiProviderModelOption(id, id);
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value) && !item.Value.StartsWith("whisper", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(item => item.Value)
            .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (items.Length == 0)
            throw new InvalidOperationException("No OpenAI models were returned for this API key.");

        return items;
    }

    private async Task<IReadOnlyList<AiProviderModelOption>> GetOpenRouterModelsAsync(
        AiProviderConnectionSettings connection,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();

        using (var keyRequest = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/key"))
        {
            keyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);

            using var keyResponse = await SendWithRateLimitRetryAsync(client, keyRequest, ct);
            var keyPayload = await keyResponse.Content.ReadAsStringAsync(ct);
            keyResponse.EnsureSuccessStatusCode();

            using var keyDocument = JsonDocument.Parse(keyPayload);
            if (!keyDocument.RootElement.TryGetProperty("data", out _))
            {
                throw new InvalidOperationException("OpenRouter API key validation failed.");
            }
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.ApiKey);

        using var response = await SendWithRateLimitRetryAsync(client, request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(payload);
        var items = document.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(item =>
            {
                var id = item.GetProperty("id").GetString() ?? string.Empty;
                var label = item.TryGetProperty("name", out var name)
                    ? name.GetString() ?? id
                    : id;
                return new AiProviderModelOption(id, label);
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .DistinctBy(item => item.Value)
            .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (items.Length == 0)
            throw new InvalidOperationException("No OpenRouter models were returned for this API key.");

        return items;
    }

    private static RegulationSuggestionResult ParseSuggestionResult(string rawJson, string provider)
    {
        var json = ExtractJson(rawJson);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var description = GetString(root, "description");
        var condition = GetString(root, "condition");
        var suggestedTask = GetString(root, "suggestedTask");
        var frequencyType = NormalizeAllowedValue(GetString(root, "frequencyType"), FrequencyTypes, "Monthly");
        var dueDateType = NormalizeAllowedValue(GetString(root, "dueDateType"), DueDateTypes, "Day of Month");
        // Prompt instructs the model to ALWAYS return numbers (using 0/0 as a
        // sentinel when the regulation has no quantitative element). If the
        // model nevertheless returns null or omits the field, fall back to 0
        // so the form always shows a concrete value end-to-end.
        var minValue = ExtractNumericBound(root, isMin: true) ?? 0m;
        var maxValue = ExtractNumericBound(root, isMin: false) ?? 0m;

        return new RegulationSuggestionResult(
            description, condition, suggestedTask, frequencyType, dueDateType, provider, minValue, maxValue);
    }

    private static string BuildPrompt(RegulationSuggestionContext context)
    {
        // Pass the full regulation text through (stripped of HTML markup). The
        // upstream "summary" used to truncate at 1200 chars which dropped
        // critical condition language for any regulation longer than a few
        // paragraphs. We raise this to 16000 chars (~4000 tokens) so the model
        // sees the actual prescriptive text.
        var content = BuildCleanContent(context.HtmlContent);

        var subscriptionScope = context.SubscribingLevel is not null
            ? $"- Subscribing Level: {context.SubscribingLevel}\n- Subscribed Node: {context.SubscribedNodeName ?? "(unspecified)"}"
            : "- Subscribing Level: (regulation-level legacy flow)";

        return $$"""
You are a regulatory compliance assistant. Your job is to produce machine-actionable
task metadata for a compliance subscription based on the full regulation text.

Subscription Context:
{{subscriptionScope}}

Regulation Context:
- Federal Title: {{context.TitleName}}
- Section Number: {{context.SectionNumber}}
- Section Name: {{context.SectionName}}

Full Section Text (HTML stripped):
--- BEGIN SECTION TEXT ---
{{content}}
--- END SECTION TEXT ---

Existing Draft Values (refine these; do not blindly overwrite if already good):
- description: {{context.ExistingDescription ?? "(empty)"}}
- condition: {{context.ExistingCondition ?? "(empty)"}}
- suggestedTask: {{context.ExistingSuggestedTask ?? "(empty)"}}
- minValue: {{context.ExistingMinValue?.ToString() ?? "(empty)"}}
- maxValue: {{context.ExistingMaxValue?.ToString() ?? "(empty)"}}

Allowed Dropdown Values:

frequencyType (pick exactly one):
- Daily
- Weekly
- Bi-Weekly
- Monthly
- Quarterly
- Semi-Annually
- Annually
- One-Time

dueDateType (pick exactly one):
- Day of Month
- Day of Week
- Fixed Date
- Relative to Start
- End of Month
- Immediate
- Milestone Based

Required Output JSON Schema (return STRICT JSON, no prose, no markdown fences):
{
  "description":   string,   // 1-3 sentences explaining what this regulation requires of the regulated party
  "condition":     string,   // the trigger condition that activates the obligation, written precisely
  "suggestedTask": string,   // an actionable task the customer should perform to stay compliant
  "frequencyType": string,   // EXACTLY one of the allowed values above
  "dueDateType":   string,   // EXACTLY one of the allowed values above
  "minValue":      number,   // ALWAYS a JSON number — never null, never a string. See rules below.
  "maxValue":      number    // ALWAYS a JSON number — never null, never a string. See rules below.
}

Rules for minValue / maxValue (READ CAREFULLY — these fields are MANDATORY numbers):
- minValue and maxValue MUST be present and MUST be JSON numbers. NEVER null. NEVER strings.
- Extract numeric bounds whenever the regulation or condition contains any quantitative
  limit, threshold, deadline, count, percentage, concentration, duration, or measurement.
- If only one side of the bound is stated, copy the stated bound to the missing side
  (e.g. "at least 100" -> minValue: 100, maxValue: 100).
- If the regulation is purely procedural/administrative with NO quantitative element at all
  (e.g. 1 CFR publication procedure, definitions, general provisions), return minValue: 0
  and maxValue: 0 as the sentinel meaning "not applicable".
- Use the unit implied by the condition (percent, days, ppm, count). Do NOT include the unit
  in the JSON value — just the number.

Few-shot examples (input -> output):

Example A — emissions threshold:
  Section text: "Opacity from any affected facility shall not exceed 20 percent."
  -> "condition": "Opacity from any affected facility exceeds 20 percent.",
     "minValue": 0, "maxValue": 20

Example B — reporting deadline:
  Section text: "The owner shall submit a written report within 30 days of the event."
  -> "condition": "An event occurs requiring a written report.",
     "minValue": 0, "maxValue": 30

Example C — concentration range:
  Section text: "Carbon monoxide levels must be maintained between 5 and 25 ppm during operations."
  -> "condition": "Carbon monoxide levels measured during operations.",
     "minValue": 5, "maxValue": 25

Example D — one-sided lower bound:
  Section text: "Storage tanks shall hold no less than 100 gallons of secondary containment capacity."
  -> "condition": "Secondary containment capacity falls below 100 gallons.",
     "minValue": 100, "maxValue": 100

Example E — count-per-year frequency:
  Section text: "Reports shall be submitted quarterly."
  -> "condition": "End of each quarter.",
     "frequencyType": "Quarterly",
     "minValue": 4, "maxValue": 4

Example F — purely procedural (no quantitative content):
  Section text: "The Director of the Federal Register shall periodically publish the CFR."
  -> "condition": "A new CFR edition is published.",
     "minValue": 0, "maxValue": 0          // sentinel: not applicable

Other rules:
- Use only the provided dropdown values verbatim for frequencyType / dueDateType.
- Use only the section text above. Do not invent regulations.
- Keep all strings concise and professional.
- Return STRICT JSON only — no surrounding text, no markdown.
""";
    }

    private static string BuildCleanContent(string? htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return "(No section text available for this subscription scope.)";

        var text = Regex.Replace(htmlContent, "<.*?>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        // 16k chars (~4k tokens) is enough headroom for typical CFR sections
        // while keeping the request well under provider context limits.
        const int maxChars = 16000;
        return text.Length <= maxChars ? text : text[..maxChars] + " ...[truncated]";
    }

    /// <summary>
    /// Pull a min or max numeric bound out of the LLM response. Robust against
    /// the four shapes models actually return in the wild:
    ///   1. {"minValue": 20}                    — canonical, requested shape
    ///   2. {"min_value": 20} or {"min": 20}    — alternate property names
    ///   3. {"minValue": "20%"} / "30 days"     — number-in-string with unit suffix
    ///   4. {"dataRange": {"min": 0, "max": 20}} — nested under a wrapper object
    /// </summary>
    private static decimal? ExtractNumericBound(JsonElement root, bool isMin)
    {
        var primaryNames = isMin
            ? new[] { "minValue", "min_value", "min", "lowerBound", "lower_bound" }
            : new[] { "maxValue", "max_value", "max", "upperBound", "upper_bound" };

        // Direct top-level lookup under any of the aliases.
        foreach (var name in primaryNames)
        {
            if (TryReadNumericValue(root, name, out var direct))
                return direct;
        }

        // Nested under a wrapper object the model may have invented from the
        // "Data Range" form label (e.g. dataRange / range / bounds / limit).
        var wrapperNames = new[] { "dataRange", "data_range", "range", "bounds", "limit", "limits", "threshold", "thresholds" };
        var innerNames = isMin
            ? new[] { "min", "minValue", "min_value", "from", "lower" }
            : new[] { "max", "maxValue", "max_value", "to", "upper" };

        foreach (var wrapper in wrapperNames)
        {
            if (!root.TryGetProperty(wrapper, out var nested) || nested.ValueKind != JsonValueKind.Object)
                continue;
            foreach (var inner in innerNames)
            {
                if (TryReadNumericValue(nested, inner, out var nestedVal))
                    return nestedVal;
            }
        }

        return null;
    }

    private static bool TryReadNumericValue(JsonElement parent, string propertyName, out decimal value)
    {
        value = 0;
        if (!parent.TryGetProperty(propertyName, out var property))
            return false;

        switch (property.ValueKind)
        {
            case JsonValueKind.Null:
                return false;

            case JsonValueKind.Number:
                return property.TryGetDecimal(out value);

            case JsonValueKind.String:
                {
                    var s = property.GetString();
                    if (string.IsNullOrWhiteSpace(s)) return false;
                    if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
                        return true;
                    // String with unit suffix: pull the first numeric token.
                    // Handles "20%", "30 days", "100 ppm", "0.5 mg/L", "-10".
                    var match = System.Text.RegularExpressions.Regex.Match(s, @"-?\d+(\.\d+)?");
                    if (match.Success && decimal.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
                        return true;
                    return false;
                }

            default:
                return false;
        }
    }

    private static string ExtractJson(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            return trimmed;

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed[start..(end + 1)];

        throw new InvalidOperationException("AI response was not valid JSON.");
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            throw new InvalidOperationException($"AI response is missing '{propertyName}'.");

        var value = property.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"AI response has an empty '{propertyName}'.");

        return value;
    }

    private static string NormalizeAllowedValue(string value, string[] allowedValues, string fallback)
    {
        var match = allowedValues.FirstOrDefault(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
        return match ?? fallback;
    }

    private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken ct)
    {
        const int maxAttempts = 3;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var clonedRequest = await CloneHttpRequestMessageAsync(request, ct);
            var response = await client.SendAsync(clonedRequest, ct);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "AI provider returned {Status} ({StatusCode}). Raw body: {Body}",
                    response.StatusCode, (int)response.StatusCode, errorBody);
                var message = BuildProviderErrorMessage(response.StatusCode, errorBody);
                response.Dispose();
                throw new InvalidOperationException(message);
            }

            if (attempt == maxAttempts)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                var retryAfter = response.Headers.RetryAfter?.Delta;
                response.Dispose();
                throw new InvalidOperationException(
                    retryAfter is { } waitFor
                        ? $"The AI provider is rate-limiting requests. Please retry after {Math.Ceiling(waitFor.TotalSeconds)} seconds. {ExtractProviderErrorDetail(errorBody)}".Trim()
                        : $"The AI provider is rate-limiting requests. Please wait a moment and try again. {ExtractProviderErrorDetail(errorBody)}".Trim());
            }

            var wait = response.Headers.RetryAfter?.Delta ?? delay;
            response.Dispose();
            await Task.Delay(wait, ct);
            delay += delay;
        }

        throw new InvalidOperationException("Unable to complete the AI provider request.");
    }

    private static string BuildProviderErrorMessage(HttpStatusCode statusCode, string errorBody)
    {
        var detail = ExtractProviderErrorDetail(errorBody);
        var friendlyMessage = BuildFriendlyProviderErrorMessage(statusCode, detail);

        if (!string.IsNullOrWhiteSpace(friendlyMessage))
            return friendlyMessage;

        var fallback = $"The AI provider request failed with status {(int)statusCode}.";
        return string.IsNullOrWhiteSpace(detail) ? fallback : $"{fallback} {detail}";
    }

    private static string ExtractProviderErrorDetail(string? errorBody)
    {
        if (string.IsNullOrWhiteSpace(errorBody))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(errorBody);
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString()?.Trim() ?? string.Empty;

                if (error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("metadata", out var metadata) &&
                        metadata.ValueKind == JsonValueKind.Object &&
                        metadata.TryGetProperty("raw", out var raw))
                    {
                        return raw.GetString()?.Trim() ?? string.Empty;
                    }

                    if (error.TryGetProperty("message", out var message))
                        return message.GetString()?.Trim() ?? string.Empty;

                    if (error.TryGetProperty("code", out var code))
                        return code.GetString()?.Trim() ?? string.Empty;

                    if (error.TryGetProperty("status", out var status))
                        return status.GetString()?.Trim() ?? string.Empty;
                }
            }

            if (root.TryGetProperty("detail", out var detail))
                return detail.GetString()?.Trim() ?? string.Empty;

            if (root.TryGetProperty("error_message", out var errorMessage))
                return errorMessage.GetString()?.Trim() ?? string.Empty;

            if (root.TryGetProperty("message", out var rootMessage))
                return rootMessage.GetString()?.Trim() ?? string.Empty;
        }
        catch (JsonException)
        {
        }

        return errorBody.Trim();
    }

    private static string? BuildFriendlyProviderErrorMessage(HttpStatusCode statusCode, string detail)
    {
        var normalizedDetail = detail.Trim();
        var lowerDetail = normalizedDetail.ToLowerInvariant();

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "The AI provider credentials are invalid or do not have access. Please check the API key and try again.";
        }

        if (statusCode == HttpStatusCode.BadRequest)
        {
            if (lowerDetail.Contains("api key") ||
                lowerDetail.Contains("invalid key") ||
                lowerDetail.Contains("incorrect api key") ||
                lowerDetail.Contains("authentication") ||
                lowerDetail.Contains("unauthorized") ||
                lowerDetail.Contains("auth"))
            {
                return "The AI provider credentials are invalid. Please verify the API key and try again.";
            }

            if (lowerDetail.Contains("endpoint"))
            {
                return "The AI provider endpoint is invalid. Please verify the endpoint and try again.";
            }

            if (lowerDetail.Contains("model"))
            {
                return "The selected model is not available for these AI provider credentials. Please choose a different model.";
            }

            if (lowerDetail.Contains("max_tokens") ||
                lowerDetail.Contains("context length") ||
                lowerDetail.Contains("context_length") ||
                lowerDetail.Contains("too long") ||
                lowerDetail.Contains("token limit") ||
                lowerDetail.Contains("prompt is too long"))
            {
                return $"The regulation text is too large for the selected model's context window. Try a model with a larger context (e.g. claude-3-5-sonnet-latest, gpt-4o, gemini-1.5-pro). Provider said: {normalizedDetail}";
            }

            // Don't hide the provider's actual message — it's the most useful
            // signal for diagnosing a config / payload problem.
            return string.IsNullOrWhiteSpace(normalizedDetail)
                ? "The AI provider rejected the request (HTTP 400) but did not return a reason."
                : $"The AI provider rejected the request (HTTP 400): {normalizedDetail}";
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return "The AI provider is rate-limiting requests. Please wait a moment and try again.";
        }

        return null;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        clone.Version = request.Version;
        clone.VersionPolicy = request.VersionPolicy;
        return clone;
    }
}
