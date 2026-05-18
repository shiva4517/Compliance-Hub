using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;
using Microsoft.Extensions.Configuration;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Ai;

/// <summary>
/// Shared implementation for any provider that speaks the OpenAI
/// <c>chat/completions</c> protocol (OpenAI, Google Gemini via its
/// OpenAI-compatible endpoint, Groq, OpenRouter, local Ollama, ...).
/// Concrete providers only supply config keys, default model and HTTP client name.
/// </summary>
internal abstract class OpenAiCompatibleAiPrModelProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ProcessCommandRunner commandRunner) : IAiPrModelProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public abstract AiPrModelProviderKind Kind { get; }

    /// <summary>Named HttpClient (registered in DI) whose BaseAddress points at the provider.</summary>
    protected abstract string HttpClientName { get; }

    /// <summary>Configuration prefix, e.g. <c>AiPrAutomation:Gemini</c>.</summary>
    protected abstract string ConfigPrefix { get; }

    /// <summary>Model used when none is configured.</summary>
    protected abstract string DefaultModel { get; }

    /// <summary>Environment variables checked (in order) for the API key.</summary>
    protected abstract IReadOnlyList<string> ApiKeyEnvironmentVariables { get; }

    /// <summary>Human-readable provider name used in status messages.</summary>
    protected string ProviderName => Kind.ToString();

    public async Task<CodeGenerationPlan> GenerateCodePlanAsync(StartAiPrAutomationRequest request, CancellationToken ct)
    {
        var prompt = $"""
        Create a concise implementation plan for this software task.

        Repository: {request.RepositoryOwner}/{request.RepositoryName}
        Base branch: {request.BaseBranch}
        Title: {request.TaskTitle}
        Description:
        {request.TaskDescription}

        Return plain text with target areas and validation commands.
        """;

        var content = await SendPromptAsync(prompt, ct);
        var commands = request.ValidationCommands.Count > 0 ? request.ValidationCommands : new[] { "dotnet build" };

        return new CodeGenerationPlan(content, new[] { "AI-selected implementation areas" }, commands);
    }

    public async Task<CodeGenerationResult> GenerateCodeChangesAsync(StartAiPrAutomationRequest request, CodeGenerationPlan plan, string repositoryPath, CancellationToken ct)
    {
        var repositorySnapshot = BuildRepositorySnapshot(repositoryPath);
        var prompt = $"""
        You are the AI coding agent. Generate a unified git diff patch that implements the requested task.

        Rules:
        - Return only a unified diff that can be applied with git apply.
        - Use standard "diff --git" headers with a/ and b/ prefixes and correct hunk counts.
        - Do not wrap the diff in prose unless no safe change can be produced.
        - Keep the patch minimal and focused.
        - Do not modify secrets, build outputs, bin/obj, node_modules, or .git files.

        Task title: {request.TaskTitle}
        Task description:
        {request.TaskDescription}

        Plan:
        {plan.Summary}

        Repository snapshot:
        {repositorySnapshot}
        """;

        var response = await SendPromptAsync(prompt, ct);
        var patch = ExtractPatch(response);
        if (string.IsNullOrWhiteSpace(patch))
        {
            return new CodeGenerationResult($"{ProviderName} did not return an applicable patch.", Array.Empty<string>(), HasChanges: false);
        }

        var applied = await ApplyPatchAsync(repositoryPath, patch, ct);
        if (!applied.Succeeded)
        {
            return new CodeGenerationResult($"{ProviderName} returned a patch, but git apply failed: {applied.ErrorOutput}{applied.Output}", Array.Empty<string>(), HasChanges: false);
        }

        var changedFiles = await GetChangedFilesAsync(repositoryPath, ct);
        return new CodeGenerationResult($"{ProviderName} generated and applied code changes.", changedFiles, changedFiles.Count > 0);
    }

    public async Task<PullRequestReviewResult> ReviewPullRequestAsync(StartAiPrAutomationRequest request, IReadOnlyList<ChangedFile> changedFiles, string diff, CancellationToken ct)
    {
        var prompt = $"""
        Review this pull request diff for bugs, security issues, performance problems, coding standards,
        architecture violations, missing validations, and test coverage gaps.

        Task:
        {request.TaskDescription}

        Changed files:
        {string.Join(Environment.NewLine, changedFiles.Select(f => $"- {f.Path} ({f.Status}, +{f.Additions}/-{f.Deletions})"))}

        Diff:
        {Truncate(diff, 60000)}

        Write actionable review findings. On the FINAL line output exactly one of:
        DECISION: APPROVED
        DECISION: CHANGES_REQUESTED
        Use APPROVED only when there are no blocking findings.
        """;

        var content = await SendPromptAsync(prompt, ct);
        var decision = ParseReviewDecision(content);

        return decision == PullRequestReviewDecision.Approved
            ? new PullRequestReviewResult(PullRequestReviewDecision.Approved, content, Array.Empty<ReviewCommentRequest>())
            : new PullRequestReviewResult(
                PullRequestReviewDecision.ChangesRequested,
                content,
                new[] { new ReviewCommentRequest(content) });
    }

    public async Task<ReviewFixResult> FixReviewCommentsAsync(StartAiPrAutomationRequest request, IReadOnlyList<PullRequestComment> comments, string repositoryPath, CancellationToken ct)
    {
        var repositorySnapshot = BuildRepositorySnapshot(repositoryPath);
        var currentDiff = await GetCurrentDiffAsync(repositoryPath, ct);
        var prompt = $"""
        You are the AI fix agent. Generate a unified git diff patch that resolves these PR review comments.

        Rules:
        - Return only a unified diff that can be applied with git apply.
        - Use standard "diff --git" headers with a/ and b/ prefixes and correct hunk counts.
        - Keep the patch minimal and focused on the review comments.
        - Do not modify secrets, build outputs, bin/obj, node_modules, or .git files.
        - If no safe code change can be produced, return NO_PATCH and explain briefly.

        Original task:
        {request.TaskDescription}

        Review comments:
        {string.Join(Environment.NewLine, comments.Select(c => $"- {c.FilePath}:{c.Line} {c.Body}"))}

        Current diff:
        {Truncate(currentDiff, 40000)}

        Repository snapshot:
        {repositorySnapshot}
        """;

        var response = await SendPromptAsync(prompt, ct);
        var patch = ExtractPatch(response);
        if (string.IsNullOrWhiteSpace(patch))
        {
            return new ReviewFixResult($"{ProviderName} did not return an applicable fix patch.", Array.Empty<string>(), HasChanges: false);
        }

        var applied = await ApplyPatchAsync(repositoryPath, patch, ct);
        if (!applied.Succeeded)
        {
            return new ReviewFixResult($"{ProviderName} returned a fix patch, but git apply failed: {applied.ErrorOutput}{applied.Output}", Array.Empty<string>(), HasChanges: false);
        }

        var changedFiles = await GetChangedFilesAsync(repositoryPath, ct);
        return new ReviewFixResult($"{ProviderName} applied fixes for review comments.", changedFiles, changedFiles.Count > 0);
    }

    public async Task<string> SummarizeValidationFailureAsync(StartAiPrAutomationRequest request, IReadOnlyList<ValidationResult> validationResults, CancellationToken ct)
    {
        var prompt = $"""
        Summarize these validation failures and recommend next fixes.

        Task:
        {request.TaskDescription}

        Results:
        {string.Join(Environment.NewLine, validationResults.Select(r => $"{r.Command}: exit {r.ExitCode}\n{Truncate(r.Output + r.ErrorOutput, 8000)}"))}
        """;

        return await SendPromptAsync(prompt, ct);
    }

    /// <summary>
    /// Strict decision parsing: only an explicit "DECISION: APPROVED" token approves.
    /// Anything else (CHANGES_REQUESTED, or a missing/garbled token) requests changes.
    /// </summary>
    internal static PullRequestReviewDecision ParseReviewDecision(string content)
    {
        var match = Regex.Matches(content ?? string.Empty, @"DECISION:\s*(APPROVED|CHANGES_REQUESTED)", RegexOptions.IgnoreCase)
            .Cast<Match>()
            .LastOrDefault();

        return match is not null && match.Groups[1].Value.Equals("APPROVED", StringComparison.OrdinalIgnoreCase)
            ? PullRequestReviewDecision.Approved
            : PullRequestReviewDecision.ChangesRequested;
    }

    private async Task<ValidationResult> ApplyPatchAsync(string repositoryPath, string patch, CancellationToken ct)
    {
        var patchPath = Path.Combine(Path.GetTempPath(), $"aipr-{Guid.NewGuid():N}.patch");
        await File.WriteAllTextAsync(patchPath, patch, ct);
        try
        {
            // LLM-produced diffs frequently have minor context/whitespace/line-count drift.
            // Try progressively more tolerant apply strategies before giving up.
            var attempts = new[]
            {
                $"git apply --whitespace=fix \"{patchPath}\"",
                $"git apply --whitespace=fix --recount \"{patchPath}\"",
                $"git apply --3way \"{patchPath}\"",
            };

            ValidationResult last = null!;
            foreach (var command in attempts)
            {
                last = await commandRunner.RunAsync(repositoryPath, command, ct);
                if (last.Succeeded)
                {
                    return last;
                }
            }

            return last;
        }
        finally
        {
            if (File.Exists(patchPath))
            {
                File.Delete(patchPath);
            }
        }
    }

    private async Task<IReadOnlyList<string>> GetChangedFilesAsync(string repositoryPath, CancellationToken ct)
    {
        var result = await commandRunner.RunAsync(repositoryPath, "git status --short", ct);
        if (!result.Succeeded)
        {
            return Array.Empty<string>();
        }

        return result.Output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Length > 3 ? line[3..].Trim() : line.Trim())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<string> GetCurrentDiffAsync(string repositoryPath, CancellationToken ct)
    {
        var result = await commandRunner.RunAsync(repositoryPath, "git diff -- .", ct);
        return result.Output + result.ErrorOutput;
    }

    private static string BuildRepositorySnapshot(string repositoryPath)
    {
        var root = new DirectoryInfo(repositoryPath);
        if (!root.Exists)
        {
            return "Repository workspace is empty.";
        }

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", "bin", "obj", "node_modules", "dist", "build" };
        var files = root.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(file => !file.FullName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(excluded.Contains))
            .Where(file => file.Length <= 80_000)
            .Take(80)
            .Select(file =>
            {
                var relative = Path.GetRelativePath(repositoryPath, file.FullName);
                var content = File.ReadAllText(file.FullName);
                return $"--- {relative} ---\n{Truncate(content, 5000)}";
            });

        return string.Join("\n", files);
    }

    private static string ExtractPatch(string response)
    {
        var trimmed = response.Trim();
        if (trimmed.Contains("NO_PATCH", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var fenceStart = trimmed.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var contentStart = trimmed.IndexOf('\n', fenceStart);
            var fenceEnd = contentStart >= 0 ? trimmed.IndexOf("```", contentStart + 1, StringComparison.Ordinal) : -1;
            if (contentStart >= 0 && fenceEnd > contentStart)
            {
                trimmed = trimmed[(contentStart + 1)..fenceEnd].Trim();
            }
        }

        var diffStart = trimmed.IndexOf("diff --git", StringComparison.Ordinal);
        return diffStart >= 0 ? trimmed[diffStart..].Trim() : string.Empty;
    }

    private async Task<string> SendPromptAsync(string prompt, CancellationToken ct)
    {
        var apiKey = configuration[$"{ConfigPrefix}:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            foreach (var variable in ApiKeyEnvironmentVariables)
            {
                apiKey = Environment.GetEnvironmentVariable(variable);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"{ProviderName} API key is required. Set {ConfigPrefix}:ApiKey or one of: {string.Join(", ", ApiKeyEnvironmentVariables)}.");
        }

        var model = configuration[$"{ConfigPrefix}:Model"] ?? DefaultModel;
        var client = httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.PostAsJsonAsync(
            "chat/completions",
            new
            {
                model,
                max_tokens = 4096,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            },
            JsonOptions,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"{ProviderName} AI PR request failed. Status: {(int)response.StatusCode}. Response: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException($"{ProviderName} response was empty.");

        return string.Join(
            Environment.NewLine,
            payload.Choices
                .Select(choice => choice.Message?.Content)
                .Where(text => !string.IsNullOrWhiteSpace(text))!);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "\n[truncated]";

    private sealed record OpenAiChatResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChatChoice> Choices);

    private sealed record OpenAiChatChoice(
        [property: JsonPropertyName("message")] OpenAiChatMessage? Message);

    private sealed record OpenAiChatMessage(
        [property: JsonPropertyName("content")] string? Content);
}
