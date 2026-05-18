using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IGitProvider
{
    GitProviderKind Kind { get; }

    Task CloneRepositoryAsync(GitRepositoryRef repository, string localPath, CancellationToken ct);
    Task<GitBranchRef> CreateBranchAsync(GitRepositoryRef repository, string baseBranch, string branchName, CancellationToken ct);
    Task CommitChangesAsync(string localPath, string message, CancellationToken ct);
    Task PushChangesAsync(string localPath, string branchName, CancellationToken ct);
    Task<PullRequestRef> CreatePullRequestAsync(GitRepositoryRef repository, string sourceBranch, string targetBranch, string title, string body, CancellationToken ct);
    Task<PullRequestDetails> GetPullRequestAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct);
    Task<IReadOnlyList<ChangedFile>> GetChangedFilesAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct);
    Task<string> GetDiffAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct);
    Task<IReadOnlyList<PullRequestComment>> GetReviewCommentsAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct);
    Task AddReviewCommentAsync(GitRepositoryRef repository, int pullRequestNumber, ReviewCommentRequest comment, CancellationToken ct);

    /// <summary>Posts a raw comment body without injecting the AI-review marker (used for terminal notices).</summary>
    Task AddIssueCommentAsync(GitRepositoryRef repository, int pullRequestNumber, string body, CancellationToken ct);
    Task ApprovePullRequestAsync(GitRepositoryRef repository, int pullRequestNumber, string body, CancellationToken ct);
    Task<IReadOnlyList<WorkflowRunStatus>> GetWorkflowRunsAsync(GitRepositoryRef repository, string branchName, CancellationToken ct);
}
