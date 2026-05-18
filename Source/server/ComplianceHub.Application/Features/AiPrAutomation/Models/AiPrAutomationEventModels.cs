namespace ComplianceHub.Application.Features.AiPrAutomation.Models;

public enum AiPrAutomationEventKind
{
    PullRequestOpened = 0,
    PullRequestUpdated = 1,
    ReviewCommentCreated = 2
}

public sealed record AiPrAutomationEvent(
    AiPrAutomationEventKind Kind,
    GitProviderKind GitProvider,
    AiPrModelProviderKind AiProvider,
    string RepositoryUrl,
    string RepositoryOwner,
    string RepositoryName,
    string DefaultBranch,
    int PullRequestNumber,
    string PullRequestTitle,
    string PullRequestBody,
    string SourceBranch,
    string TargetBranch,
    string? CommentBody,
    string? CommentPath,
    int? CommentLine,
    IReadOnlyList<string> ValidationCommands);

public sealed record AiPrAutomationEventResult(
    Guid RunId,
    AiPrRunStatus Status,
    AiPrAutomationEventKind EventKind,
    string Summary,
    IReadOnlyList<string> Steps,
    IReadOnlyList<ReviewCommentRequest> ReviewComments,
    IReadOnlyList<ValidationResult> ValidationResults);
