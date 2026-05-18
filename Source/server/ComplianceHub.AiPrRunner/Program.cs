using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
}));
services.AddAiPrAutomationInfrastructure(configuration);

await using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AiPrRunner");
var orchestrator = serviceProvider.GetRequiredService<IAiPrEventOrchestrator>();

try
{
    var automationEvent = GitHubActionsEventMapper.Map(configuration);
    logger.LogInformation("AI PR runner handling {EventKind} for {Owner}/{Repo} PR #{PrNumber}.",
        automationEvent.Kind,
        automationEvent.RepositoryOwner,
        automationEvent.RepositoryName,
        automationEvent.PullRequestNumber);

    var result = await orchestrator.HandleAsync(automationEvent, CancellationToken.None);

    logger.LogInformation("AI PR runner completed with status {Status}: {Summary}", result.Status, result.Summary);
    foreach (var step in result.Steps)
    {
        logger.LogInformation("{Step}", step);
    }

    if (result.Status == AiPrRunStatus.Failed)
    {
        Environment.ExitCode = 1;
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "AI PR runner failed.");
    Environment.ExitCode = 1;
}

internal static class GitHubActionsEventMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AiPrAutomationEvent Map(IConfiguration configuration)
    {
        var eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME")
            ?? throw new InvalidOperationException("GITHUB_EVENT_NAME is required.");
        var eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH")
            ?? throw new InvalidOperationException("GITHUB_EVENT_PATH is required.");

        using var document = JsonDocument.Parse(File.ReadAllText(eventPath));
        var root = document.RootElement;
        var provider = ReadEnum(configuration["AiPrAutomation:GitProvider"], GitProviderKind.GitHub);
        var aiProvider = ReadEnum(configuration["AiPrAutomation:AiProvider"], AiPrModelProviderKind.Gemini);
        var validationCommands = ReadValidationCommands(configuration);

        return eventName switch
        {
            "pull_request" => MapPullRequest(root, provider, aiProvider, validationCommands),
            "pull_request_review_comment" => MapReviewComment(root, provider, aiProvider, validationCommands),
            "issue_comment" => MapIssueComment(root, provider, aiProvider, validationCommands),
            _ => throw new NotSupportedException($"GitHub event '{eventName}' is not supported by the AI PR runner.")
        };
    }

    private static AiPrAutomationEvent MapPullRequest(
        JsonElement root,
        GitProviderKind provider,
        AiPrModelProviderKind aiProvider,
        IReadOnlyList<string> validationCommands)
    {
        var action = root.GetProperty("action").GetString() ?? string.Empty;
        var kind = action switch
        {
            "opened" or "reopened" or "ready_for_review" => AiPrAutomationEventKind.PullRequestOpened,
            "synchronize" => AiPrAutomationEventKind.PullRequestUpdated,
            _ => throw new NotSupportedException($"Pull request action '{action}' is not handled.")
        };

        var repository = root.GetProperty("repository").Deserialize<GitHubRepository>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub repository payload is missing.");
        var pullRequest = root.GetProperty("pull_request").Deserialize<GitHubPullRequest>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub pull_request payload is missing.");

        return new AiPrAutomationEvent(
            kind,
            provider,
            aiProvider,
            repository.CloneUrl,
            repository.Owner.Login,
            repository.Name,
            repository.DefaultBranch,
            pullRequest.Number,
            pullRequest.Title,
            pullRequest.Body ?? string.Empty,
            pullRequest.Head.Ref,
            pullRequest.Base.Ref,
            null,
            null,
            null,
            validationCommands);
    }

    private static AiPrAutomationEvent MapReviewComment(
        JsonElement root,
        GitProviderKind provider,
        AiPrModelProviderKind aiProvider,
        IReadOnlyList<string> validationCommands)
    {
        var action = root.GetProperty("action").GetString() ?? string.Empty;
        if (action != "created")
        {
            throw new NotSupportedException($"Review comment action '{action}' is not handled.");
        }

        var repository = root.GetProperty("repository").Deserialize<GitHubRepository>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub repository payload is missing.");
        var pullRequest = root.GetProperty("pull_request").Deserialize<GitHubPullRequest>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub pull_request payload is missing.");
        var comment = root.GetProperty("comment").Deserialize<GitHubComment>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub comment payload is missing.");

        return new AiPrAutomationEvent(
            AiPrAutomationEventKind.ReviewCommentCreated,
            provider,
            aiProvider,
            repository.CloneUrl,
            repository.Owner.Login,
            repository.Name,
            repository.DefaultBranch,
            pullRequest.Number,
            pullRequest.Title,
            pullRequest.Body ?? string.Empty,
            pullRequest.Head.Ref,
            pullRequest.Base.Ref,
            comment.Body,
            comment.Path,
            comment.Line ?? comment.OriginalLine,
            validationCommands);
    }


    private static AiPrAutomationEvent MapIssueComment(
        JsonElement root,
        GitProviderKind provider,
        AiPrModelProviderKind aiProvider,
        IReadOnlyList<string> validationCommands)
    {
        var action = root.GetProperty("action").GetString() ?? string.Empty;
        if (action != "created")
        {
            throw new NotSupportedException($"Issue comment action '{action}' is not handled.");
        }

        var issue = root.GetProperty("issue").Deserialize<GitHubIssue>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub issue payload is missing.");
        if (issue.PullRequest is null)
        {
            throw new NotSupportedException("Issue comment is not on a pull request.");
        }

        var repository = root.GetProperty("repository").Deserialize<GitHubRepository>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub repository payload is missing.");
        var comment = root.GetProperty("comment").Deserialize<GitHubComment>(JsonOptions)
            ?? throw new InvalidOperationException("GitHub comment payload is missing.");

        return new AiPrAutomationEvent(
            AiPrAutomationEventKind.ReviewCommentCreated,
            provider,
            aiProvider,
            repository.CloneUrl,
            repository.Owner.Login,
            repository.Name,
            repository.DefaultBranch,
            issue.Number,
            issue.Title,
            issue.Body ?? issue.Title,
            string.Empty,
            repository.DefaultBranch,
            comment.Body,
            null,
            null,
            validationCommands);
    }
    private static IReadOnlyList<string> ReadValidationCommands(IConfiguration configuration)
    {
        var raw = configuration["AiPrAutomation:ValidationCommands"]
            ?? Environment.GetEnvironmentVariable("AIPR_VALIDATION_COMMANDS");

        if (string.IsNullOrWhiteSpace(raw))
        {
            return new[] { "dotnet build" };
        }

        return raw
            .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(command => !string.IsNullOrWhiteSpace(command))
            .ToArray();
    }

    private static TEnum ReadEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct
        => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    private sealed record GitHubRepository(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("clone_url")] string CloneUrl,
        [property: JsonPropertyName("default_branch")] string DefaultBranch,
        [property: JsonPropertyName("owner")] GitHubOwner Owner);

    private sealed record GitHubOwner([property: JsonPropertyName("login")] string Login);

    private sealed record GitHubPullRequest(
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("head")] GitHubBranch Head,
        [property: JsonPropertyName("base")] GitHubBranch Base);

    private sealed record GitHubBranch([property: JsonPropertyName("ref")] string Ref);

    private sealed record GitHubIssue(
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("pull_request")] JsonElement? PullRequest);

    private sealed record GitHubComment(
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("line")] int? Line,
        [property: JsonPropertyName("original_line")] int? OriginalLine);
}
