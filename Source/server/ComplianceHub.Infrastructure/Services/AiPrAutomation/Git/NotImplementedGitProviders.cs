using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Git;

internal abstract class NotImplementedGitProvider(GitProviderKind kind) : IGitProvider
{
    public GitProviderKind Kind { get; } = kind;

    public Task CloneRepositoryAsync(GitRepositoryRef repository, string localPath, CancellationToken ct) => NotSupported();
    public Task<GitBranchRef> CreateBranchAsync(GitRepositoryRef repository, string baseBranch, string branchName, CancellationToken ct) => NotSupported<GitBranchRef>();
    public Task CommitChangesAsync(string localPath, string message, CancellationToken ct) => NotSupported();
    public Task PushChangesAsync(string localPath, string branchName, CancellationToken ct) => NotSupported();
    public Task<PullRequestRef> CreatePullRequestAsync(GitRepositoryRef repository, string sourceBranch, string targetBranch, string title, string body, CancellationToken ct) => NotSupported<PullRequestRef>();
    public Task<PullRequestDetails> GetPullRequestAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct) => NotSupported<PullRequestDetails>();
    public Task<IReadOnlyList<ChangedFile>> GetChangedFilesAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct) => NotSupported<IReadOnlyList<ChangedFile>>();
    public Task<string> GetDiffAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct) => NotSupported<string>();
    public Task<IReadOnlyList<PullRequestComment>> GetReviewCommentsAsync(GitRepositoryRef repository, int pullRequestNumber, CancellationToken ct) => NotSupported<IReadOnlyList<PullRequestComment>>();
    public Task AddReviewCommentAsync(GitRepositoryRef repository, int pullRequestNumber, ReviewCommentRequest comment, CancellationToken ct) => NotSupported();
    public Task AddIssueCommentAsync(GitRepositoryRef repository, int pullRequestNumber, string body, CancellationToken ct) => NotSupported();
    public Task ApprovePullRequestAsync(GitRepositoryRef repository, int pullRequestNumber, string body, CancellationToken ct) => NotSupported();
    public Task<IReadOnlyList<WorkflowRunStatus>> GetWorkflowRunsAsync(GitRepositoryRef repository, string branchName, CancellationToken ct) => NotSupported<IReadOnlyList<WorkflowRunStatus>>();

    private Task NotSupported()
        => Task.FromException(new NotSupportedException($"Git provider '{Kind}' is not implemented yet."));

    private Task<T> NotSupported<T>()
        => Task.FromException<T>(new NotSupportedException($"Git provider '{Kind}' is not implemented yet."));
}

internal sealed class AzureDevOpsGitProvider() : NotImplementedGitProvider(GitProviderKind.AzureDevOps);
internal sealed class GitLabGitProvider() : NotImplementedGitProvider(GitProviderKind.GitLab);
internal sealed class BitbucketGitProvider() : NotImplementedGitProvider(GitProviderKind.Bitbucket);
internal sealed class SelfHostedGitProvider() : NotImplementedGitProvider(GitProviderKind.SelfHosted);
