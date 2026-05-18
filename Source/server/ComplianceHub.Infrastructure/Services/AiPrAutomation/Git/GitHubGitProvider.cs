using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Git;

internal sealed class GitHubGitProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IOptions<AiPrAutomationOptions> options,
    ProcessCommandRunner commandRunner) : IGitProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GitProviderKind Kind => GitProviderKind.GitHub;

    public async Task CloneRepositoryAsync(GitRepositoryRef repository, string localPath, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            Directory.CreateDirectory(localPath);
            return;
        }

        var cloneUrl = BuildAuthenticatedCloneUrl(repository.RepositoryUrl);
        var result = await commandRunner.RunAsync(Directory.GetCurrentDirectory(), $"git clone {cloneUrl} \"{localPath}\"", ct);
        EnsureCommandSucceeded(result, "clone repository");
    }

    public async Task<GitBranchRef> CreateBranchAsync(GitRepositoryRef repository, string baseBranch, string branchName, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return new GitBranchRef(branchName, "dry-run-sha");
        }

        var client = CreateClient();
        var baseRef = await client.GetFromJsonAsync<GitHubRefResponse>(
            $"repos/{repository.Owner}/{repository.Name}/git/ref/heads/{Uri.EscapeDataString(baseBranch)}",
            JsonOptions,
            ct) ?? throw new InvalidOperationException("GitHub base branch was not found.");

        var response = await client.PostAsJsonAsync(
            $"repos/{repository.Owner}/{repository.Name}/git/refs",
            new { @ref = $"refs/heads/{branchName}", sha = baseRef.Object.Sha },
            JsonOptions,
            ct);

        await EnsureSuccessAsync(response, "create GitHub branch", ct);
        return new GitBranchRef(branchName, baseRef.Object.Sha);
    }

    public async Task CommitChangesAsync(string localPath, string message, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return;
        }

        // CI runners have no global git identity; set a local one so the commit succeeds.
        var name = configuration["AiPrAutomation:GitHub:CommitterName"] ?? "ai-pr-automation[bot]";
        var email = configuration["AiPrAutomation:GitHub:CommitterEmail"] ?? "ai-pr-automation[bot]@users.noreply.github.com";
        EnsureCommandSucceeded(await commandRunner.RunAsync(localPath, $"git config user.name \"{EscapeForShell(name)}\"", ct), "configure committer name");
        EnsureCommandSucceeded(await commandRunner.RunAsync(localPath, $"git config user.email \"{EscapeForShell(email)}\"", ct), "configure committer email");

        EnsureCommandSucceeded(await commandRunner.RunAsync(localPath, "git add .", ct), "stage changes");
        EnsureCommandSucceeded(await commandRunner.RunAsync(localPath, $"git commit -m \"{EscapeForShell(message)}\"", ct), "commit changes");
    }

    public async Task PushChangesAsync(string localPath, string branchName, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return;
        }

        EnsureCommandSucceeded(await commandRunner.RunAsync(localPath, $"git push -u origin HEAD:{branchName}", ct), "push branch");
    }

    public async Task<PullRequestRef> CreatePullRequestAsync(GitRepositoryRef repository, string sourceBranch, string targetBranch, string title, string body, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return new PullRequestRef("dry-run", 0, $"https://github.com/{repository.Owner}/{repository.Name}/pull/dry-run", sourceBranch, targetBranch);
        }

        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            $"repos/{repository.Owner}/{repository.Name}/pulls",
            new { title, head = sourceBranch, @base = targetBranch, body },
            JsonOptions,
            ct);

        await EnsureSuccessAsync(response, "create GitHub pull request", ct);
        var pullRequest = await response.Content.ReadFromJsonAsync<GitHubPullRequestResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("GitHub pull request response was empty.");

        return new PullRequestRef(pullRequest.Id.ToString(), pullRequest.Number, pullRequest.HtmlUrl, sourceBranch, targetBranch);
    }

    public async Task<PullRequestDetails> GetPullRequestAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct)
    {
        var client = CreateClient();
        var pullRequest = await client.GetFromJsonAsync<GitHubPullRequestResponse>(
            $"repos/{repository.Owner}/{repository.Name}/pulls/{pullRequestNumber}",
            JsonOptions,
            ct) ?? throw new InvalidOperationException("GitHub pull request was not found.");

        var pullRequestRef = new PullRequestRef(
            pullRequest.Id.ToString(),
            pullRequest.Number,
            pullRequest.HtmlUrl,
            pullRequest.Head.Ref,
            pullRequest.Base.Ref);

        return new PullRequestDetails(pullRequestRef, pullRequest.Title, pullRequest.Body ?? string.Empty, pullRequest.State, pullRequest.Draft);
    }

    public async Task<IReadOnlyList<ChangedFile>> GetChangedFilesAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return Array.Empty<ChangedFile>();
        }

        var client = CreateClient();
        var files = await client.GetFromJsonAsync<IReadOnlyList<GitHubChangedFileResponse>>(
            $"repos/{repository.Owner}/{repository.Name}/pulls/{pullRequestNumber}/files",
            JsonOptions,
            ct) ?? Array.Empty<GitHubChangedFileResponse>();

        return files
            .Select(file => new ChangedFile(file.Filename, file.Status, file.Additions, file.Deletions, file.Patch))
            .ToArray();
    }

    public async Task<string> GetDiffAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return string.Empty;
        }

        var client = CreateClient("application/vnd.github.v3.diff");
        return await client.GetStringAsync($"repos/{repository.Owner}/{repository.Name}/pulls/{pullRequestNumber}", ct);
    }

    public async Task<IReadOnlyList<PullRequestComment>> GetReviewCommentsAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return Array.Empty<PullRequestComment>();
        }

        // Read from the SAME bucket the reviewer writes to (issue comments), and keep
        // only AI-authored review comments (identified by the hidden marker) so the
        // fix agent acts on real review findings, not human chatter or its own notices.
        var client = CreateClient();
        var comments = await client.GetFromJsonAsync<IReadOnlyList<GitHubIssueCommentResponse>>(
            $"repos/{repository.Owner}/{repository.Name}/issues/{pullRequestNumber}/comments",
            JsonOptions,
            ct) ?? Array.Empty<GitHubIssueCommentResponse>();

        return comments
            .Where(comment => comment.Body.Contains(AiPrAutomationMarkers.ReviewComment, StringComparison.Ordinal))
            .Select(comment => new PullRequestComment(
                comment.Id.ToString(),
                comment.Body,
                comment.User.Login,
                FilePath: null,
                Line: null,
                IsResolved: false))
            .ToArray();
    }

    public Task AddReviewCommentAsync(GitRepositoryRef repository, int pullRequestNumber, ReviewCommentRequest comment, CancellationToken ct)
    {
        // Stamp the AI-review marker so it is read back from the same bucket and counted.
        var body = $"{AiPrAutomationMarkers.ReviewComment}\n{FormatComment(comment)}";
        return AddIssueCommentAsync(repository, pullRequestNumber, body, ct);
    }

    public async Task AddIssueCommentAsync(GitRepositoryRef repository, int pullRequestNumber, string body, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return;
        }

        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            $"repos/{repository.Owner}/{repository.Name}/issues/{pullRequestNumber}/comments",
            new { body },
            JsonOptions,
            ct);

        await EnsureSuccessAsync(response, "add GitHub issue comment", ct);
    }

    public async Task ApprovePullRequestAsync(GitRepositoryRef repository, int pullRequestNumber, string body, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun || !options.Value.AllowApproval)
        {
            return;
        }

        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            $"repos/{repository.Owner}/{repository.Name}/pulls/{pullRequestNumber}/reviews",
            new { body, @event = "APPROVE" },
            JsonOptions,
            ct);

        await EnsureSuccessAsync(response, "approve GitHub pull request", ct);
    }

    public async Task<IReadOnlyList<WorkflowRunStatus>> GetWorkflowRunsAsync(GitRepositoryRef repository, string branchName, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return Array.Empty<WorkflowRunStatus>();
        }

        var client = CreateClient();
        var runs = await client.GetFromJsonAsync<GitHubWorkflowRunsResponse>(
            $"repos/{repository.Owner}/{repository.Name}/actions/runs?branch={Uri.EscapeDataString(branchName)}",
            JsonOptions,
            ct) ?? new GitHubWorkflowRunsResponse(Array.Empty<GitHubWorkflowRunResponse>());

        return runs.WorkflowRuns
            .Select(run => new WorkflowRunStatus(
                run.Id.ToString(),
                run.Name,
                run.Status,
                MapConclusion(run.Conclusion),
                string.IsNullOrWhiteSpace(run.HtmlUrl) ? null : new Uri(run.HtmlUrl)))
            .ToArray();
    }


    private string BuildAuthenticatedCloneUrl(string repositoryUrl)
    {
        var token = configuration["AiPrAutomation:GitHub:Token"]
            ?? Environment.GetEnvironmentVariable("AIPR_GITHUB_TOKEN")
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (string.IsNullOrWhiteSpace(token) || !Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return repositoryUrl;
        }

        var builder = new UriBuilder(uri)
        {
            UserName = "x-access-token",
            Password = token
        };

        return builder.Uri.ToString();
    }
    private HttpClient CreateClient(string accept = "application/vnd.github+json")
    {
        var token = configuration["AiPrAutomation:GitHub:Token"]
            ?? Environment.GetEnvironmentVariable("AIPR_GITHUB_TOKEN")
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (string.IsNullOrWhiteSpace(token) && options.Value.Mode == AiPrAutomationMode.Live)
        {
            throw new InvalidOperationException("GitHub token is required for live AI PR automation. Set AiPrAutomation:GitHub:Token or AIPR_GITHUB_TOKEN.");
        }

        var client = httpClientFactory.CreateClient("AiPrGitHub");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ComplianceHub-AiPrAutomation/1.0");

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private static WorkflowRunConclusion MapConclusion(string? conclusion) => conclusion?.ToLowerInvariant() switch
    {
        "success" => WorkflowRunConclusion.Success,
        "failure" => WorkflowRunConclusion.Failure,
        "cancelled" => WorkflowRunConclusion.Cancelled,
        "timed_out" => WorkflowRunConclusion.TimedOut,
        _ => WorkflowRunConclusion.Unknown
    };

    private static string EscapeForShell(string value) => value.Replace("\"", "\\\"");

    private static string FormatComment(ReviewCommentRequest comment)
    {
        if (string.IsNullOrWhiteSpace(comment.FilePath))
        {
            return comment.Body;
        }

        var line = comment.Line.HasValue ? $":{comment.Line}" : string.Empty;
        return $"[{comment.FilePath}{line}] {comment.Body}";
    }

    private static void EnsureCommandSucceeded(ValidationResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to {operation}. {result.ErrorOutput}{result.Output}");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to {operation}. Status: {(int)response.StatusCode}. Response: {body}");
        }
    }

    private sealed record GitHubRefResponse([property: JsonPropertyName("object")] GitHubRefObject Object);
    private sealed record GitHubRefObject([property: JsonPropertyName("sha")] string Sha);

    private sealed record GitHubPullRequestResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("head")] GitHubBranchResponse Head,
        [property: JsonPropertyName("base")] GitHubBranchResponse Base);

    private sealed record GitHubBranchResponse([property: JsonPropertyName("ref")] string Ref);

    private sealed record GitHubChangedFileResponse(
        [property: JsonPropertyName("filename")] string Filename,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("additions")] int Additions,
        [property: JsonPropertyName("deletions")] int Deletions,
        [property: JsonPropertyName("patch")] string? Patch);

    private sealed record GitHubIssueCommentResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("user")] GitHubUserResponse User);

    private sealed record GitHubUserResponse([property: JsonPropertyName("login")] string Login);

    private sealed record GitHubWorkflowRunsResponse(
        [property: JsonPropertyName("workflow_runs")] IReadOnlyList<GitHubWorkflowRunResponse> WorkflowRuns);

    private sealed record GitHubWorkflowRunResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("conclusion")] string? Conclusion,
        [property: JsonPropertyName("html_url")] string? HtmlUrl);
}
