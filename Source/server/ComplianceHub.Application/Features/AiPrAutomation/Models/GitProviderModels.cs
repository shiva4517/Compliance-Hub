namespace ComplianceHub.Application.Features.AiPrAutomation.Models;

public sealed record GitRepositoryRef(
    string RepositoryUrl,
    string Owner,
    string Name,
    string DefaultBranch);

public sealed record GitBranchRef(
    string Name,
    string Sha);

public sealed record PullRequestRef(
    string Id,
    int Number,
    string Url,
    string SourceBranch,
    string TargetBranch);

public sealed record PullRequestDetails(
    PullRequestRef Ref,
    string Title,
    string Body,
    string State,
    bool IsDraft);

public sealed record ChangedFile(
    string Path,
    string Status,
    int Additions,
    int Deletions,
    string? Patch);

public sealed record PullRequestComment(
    string Id,
    string Body,
    string Author,
    string? FilePath,
    int? Line,
    bool IsResolved);

public sealed record ReviewCommentRequest(
    string Body,
    string? FilePath = null,
    int? Line = null);

public sealed record WorkflowRunStatus(
    string Id,
    string Name,
    string Status,
    WorkflowRunConclusion Conclusion,
    Uri? Url);
