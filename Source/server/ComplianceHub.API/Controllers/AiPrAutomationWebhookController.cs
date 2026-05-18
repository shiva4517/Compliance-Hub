using System.Text.Json;
using System.Text.Json.Serialization;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.AiPrAutomation.Commands.HandleAiPrAutomationEvent;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/ai-pr-automation/webhooks")]
public sealed class AiPrAutomationWebhookController(IMediator mediator) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost("github")]
    public async Task<ActionResult<ApiResponse<AiPrAutomationEventResult>>> HandleGitHubWebhook(
        [FromHeader(Name = "X-GitHub-Event")] string? githubEvent,
        [FromBody] JsonElement payload,
        CancellationToken ct)
    {
        var automationEvent = githubEvent switch
        {
            "pull_request" => MapPullRequestEvent(payload),
            "pull_request_review_comment" => MapReviewCommentEvent(payload),
            "issue_comment" => MapIssueCommentEvent(payload),
            _ => null
        };

        if (automationEvent is null)
        {
            return Ok(ApiResponse<object>.Ok(new { }, "Webhook event ignored."));
        }

        var result = await mediator.Send(new HandleAiPrAutomationEventCommand(automationEvent), ct);
        return Ok(ApiResponse<AiPrAutomationEventResult>.Ok(result, "AI PR automation webhook handled."));
    }

    private static AiPrAutomationEvent? MapPullRequestEvent(JsonElement payload)
    {
        var body = payload.Deserialize<GitHubPullRequestWebhook>(JsonOptions)
            ?? throw new InvalidOperationException("Invalid GitHub pull request webhook payload.");

        var kind = body.Action switch
        {
            "opened" or "reopened" => AiPrAutomationEventKind.PullRequestOpened,
            "synchronize" or "ready_for_review" => AiPrAutomationEventKind.PullRequestUpdated,
            _ => (AiPrAutomationEventKind?)null
        };

        if (kind is null)
        {
            return null;
        }

        return new AiPrAutomationEvent(
            kind.Value,
            GitProviderKind.GitHub,
            AiPrModelProviderKind.Gemini,
            body.Repository.CloneUrl,
            body.Repository.Owner.Login,
            body.Repository.Name,
            body.Repository.DefaultBranch,
            body.PullRequest.Number,
            body.PullRequest.Title,
            body.PullRequest.Body ?? string.Empty,
            body.PullRequest.Head.Ref,
            body.PullRequest.Base.Ref,
            null,
            null,
            null,
            Array.Empty<string>());
    }

    private static AiPrAutomationEvent? MapReviewCommentEvent(JsonElement payload)
    {
        var body = payload.Deserialize<GitHubReviewCommentWebhook>(JsonOptions)
            ?? throw new InvalidOperationException("Invalid GitHub review comment webhook payload.");

        if (body.Action != "created")
        {
            return null;
        }

        return new AiPrAutomationEvent(
            AiPrAutomationEventKind.ReviewCommentCreated,
            GitProviderKind.GitHub,
            AiPrModelProviderKind.Gemini,
            body.Repository.CloneUrl,
            body.Repository.Owner.Login,
            body.Repository.Name,
            body.Repository.DefaultBranch,
            body.PullRequest.Number,
            body.PullRequest.Title,
            body.PullRequest.Body ?? string.Empty,
            body.PullRequest.Head.Ref,
            body.PullRequest.Base.Ref,
            body.Comment.Body,
            body.Comment.Path,
            body.Comment.Line ?? body.Comment.OriginalLine,
            Array.Empty<string>());
    }

    private static AiPrAutomationEvent? MapIssueCommentEvent(JsonElement payload)
    {
        var body = payload.Deserialize<GitHubIssueCommentWebhook>(JsonOptions)
            ?? throw new InvalidOperationException("Invalid GitHub issue comment webhook payload.");

        // issue_comment fires for both issues and PRs; only PR comments are actionable.
        if (body.Action != "created" || body.Issue.PullRequest is null)
        {
            return null;
        }

        return new AiPrAutomationEvent(
            AiPrAutomationEventKind.ReviewCommentCreated,
            GitProviderKind.GitHub,
            AiPrModelProviderKind.Gemini,
            body.Repository.CloneUrl,
            body.Repository.Owner.Login,
            body.Repository.Name,
            body.Repository.DefaultBranch,
            body.Issue.Number,
            body.Issue.Title,
            body.Issue.Body ?? body.Issue.Title,
            string.Empty,
            body.Repository.DefaultBranch,
            body.Comment.Body,
            body.Comment.Path,
            body.Comment.Line ?? body.Comment.OriginalLine,
            Array.Empty<string>());
    }

    private sealed record GitHubIssueCommentWebhook(
        [property: JsonPropertyName("action")] string Action,
        [property: JsonPropertyName("repository")] GitHubRepository Repository,
        [property: JsonPropertyName("issue")] GitHubIssue Issue,
        [property: JsonPropertyName("comment")] GitHubComment Comment);

    private sealed record GitHubIssue(
        [property: JsonPropertyName("number")] int Number,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("pull_request")] JsonElement? PullRequest);

    private sealed record GitHubPullRequestWebhook(
        [property: JsonPropertyName("action")] string Action,
        [property: JsonPropertyName("repository")] GitHubRepository Repository,
        [property: JsonPropertyName("pull_request")] GitHubPullRequest PullRequest);

    private sealed record GitHubReviewCommentWebhook(
        [property: JsonPropertyName("action")] string Action,
        [property: JsonPropertyName("repository")] GitHubRepository Repository,
        [property: JsonPropertyName("pull_request")] GitHubPullRequest PullRequest,
        [property: JsonPropertyName("comment")] GitHubComment Comment);

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

    private sealed record GitHubComment(
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("line")] int? Line,
        [property: JsonPropertyName("original_line")] int? OriginalLine);
}
