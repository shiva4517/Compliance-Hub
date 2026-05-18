using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using Microsoft.Extensions.Options;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Workflow;

internal sealed class AiPrEventOrchestrator(
    IGitProviderFactory gitProviderFactory,
    IRepositoryWorkspaceService workspaceService,
    IPullRequestReviewerAgent reviewerAgent,
    IReviewFixAgent fixAgent,
    IValidationRunner validationRunner,
    IOptions<AiPrAutomationOptions> options) : IAiPrEventOrchestrator
{
    public async Task<AiPrAutomationEventResult> HandleAsync(AiPrAutomationEvent automationEvent, CancellationToken ct)
    {
        var runId = Guid.NewGuid();
        var steps = new List<string> { $"Handling {automationEvent.Kind} event for PR #{automationEvent.PullRequestNumber}." };
        var reviewComments = new List<ReviewCommentRequest>();
        var validationResults = new List<ValidationResult>();

        if (options.Value.AllowMerge)
        {
            throw new InvalidOperationException("AI PR automation cannot run with AllowMerge enabled. Human merge is mandatory.");
        }

        return automationEvent.Kind switch
        {
            AiPrAutomationEventKind.PullRequestOpened or AiPrAutomationEventKind.PullRequestUpdated
                => await ReviewPullRequestAsync(runId, automationEvent, steps, reviewComments, validationResults, ct),
            AiPrAutomationEventKind.ReviewCommentCreated
                => await FixReviewCommentAsync(runId, automationEvent, steps, reviewComments, validationResults, ct),
            _ => new AiPrAutomationEventResult(runId, AiPrRunStatus.Failed, automationEvent.Kind, "Unsupported event.", steps, reviewComments, validationResults)
        };
    }

    private async Task<AiPrAutomationEventResult> ReviewPullRequestAsync(
        Guid runId,
        AiPrAutomationEvent automationEvent,
        List<string> steps,
        List<ReviewCommentRequest> reviewComments,
        List<ValidationResult> validationResults,
        CancellationToken ct)
    {
        var provider = gitProviderFactory.Create(automationEvent.GitProvider);
        var request = ToRequest(automationEvent);
        var repository = ToRepository(automationEvent);

        // Bot-loop guard / iteration cap: every prior AI review posts one marked
        // comment. Once we hit the configured limit, stop the review/fix cycle and
        // hand off to a human instead of looping (and burning Actions minutes / tokens).
        var priorReviews = await provider.GetReviewCommentsAsync(repository, automationEvent.PullRequestNumber, ct);
        var maxIterations = options.Value.MaxReviewFixIterations;
        if (priorReviews.Count >= maxIterations)
        {
            await provider.AddIssueCommentAsync(
                repository,
                automationEvent.PullRequestNumber,
                $"{AiPrAutomationMarkers.HumanInterventionNotice}\nAI PR automation stopped after {priorReviews.Count} review iteration(s) (limit {maxIterations}). Human review is required. No further automated reviews or fixes will run on this PR.",
                ct);
            steps.Add($"Iteration cap reached ({priorReviews.Count}/{maxIterations}). Handed off to human.");

            return new AiPrAutomationEventResult(
                runId,
                AiPrRunStatus.RequiresHumanIntervention,
                automationEvent.Kind,
                "Maximum AI review iterations reached. Human review is required.",
                steps,
                reviewComments,
                validationResults);
        }

        var changedFiles = await provider.GetChangedFilesAsync(repository, automationEvent.PullRequestNumber, ct);
        var diff = await provider.GetDiffAsync(repository, automationEvent.PullRequestNumber, ct);
        steps.Add($"Loaded PR diff with {changedFiles.Count} changed files.");

        var review = await reviewerAgent.ReviewAsync(request, changedFiles, diff, ct);
        steps.Add($"Reviewer decision: {review.Decision}. {review.Summary}");

        if (review.Decision == PullRequestReviewDecision.Approved || review.Comments.Count == 0)
        {
            var resolved = priorReviews.Count > 0
                ? $"after {priorReviews.Count} automated fix iteration(s)"
                : "on first review";
            await provider.AddIssueCommentAsync(
                repository,
                automationEvent.PullRequestNumber,
                $"{AiPrAutomationMarkers.ResolvedNotice}\n✅ All AI review findings resolved {resolved}. The PR meets the requirements. Human merge is still required (automation never merges).",
                ct);

            // Self-approval is blocked by GitHub when the token user opened the PR;
            // don't fail the whole run over it — the resolution is already posted.
            try
            {
                await provider.ApprovePullRequestAsync(repository, automationEvent.PullRequestNumber, "AI reviewer found no blocking issues. Human merge is still required.", ct);
                steps.Add("AI reviewer approved the PR.");
            }
            catch (Exception ex)
            {
                steps.Add($"Resolution posted; formal approval skipped ({ex.Message}).");
            }

            return new AiPrAutomationEventResult(
                runId,
                AiPrRunStatus.Succeeded,
                automationEvent.Kind,
                "PR reviewed and all findings resolved. Human merge remains required.",
                steps,
                reviewComments,
                validationResults);
        }

        foreach (var comment in review.Comments)
        {
            await provider.AddReviewCommentAsync(repository, automationEvent.PullRequestNumber, comment, ct);
            reviewComments.Add(comment);
        }

        steps.Add($"Added {review.Comments.Count} review comment(s).");
        return new AiPrAutomationEventResult(
            runId,
            AiPrRunStatus.RequiresHumanIntervention,
            automationEvent.Kind,
            "PR reviewed. Fix agent will run when comments are created/received.",
            steps,
            reviewComments,
            validationResults);
    }

    private async Task<AiPrAutomationEventResult> FixReviewCommentAsync(
        Guid runId,
        AiPrAutomationEvent automationEvent,
        List<string> steps,
        List<ReviewCommentRequest> reviewComments,
        List<ValidationResult> validationResults,
        CancellationToken ct)
    {
        var provider = gitProviderFactory.Create(automationEvent.GitProvider);
        var repository = ToRepository(automationEvent);

        // Bot-loop guard: never react to our own terminal "needs a human" notice.
        if (!string.IsNullOrEmpty(automationEvent.CommentBody)
            && (automationEvent.CommentBody.Contains(AiPrAutomationMarkers.HumanInterventionNotice, StringComparison.Ordinal)
                || automationEvent.CommentBody.Contains(AiPrAutomationMarkers.ResolvedNotice, StringComparison.Ordinal)))
        {
            steps.Add("Triggering comment is a terminal notice (resolved / human-needed). Skipping.");
            return new AiPrAutomationEventResult(runId, AiPrRunStatus.Succeeded, automationEvent.Kind, "No action: terminal notice comment.", steps, reviewComments, validationResults);
        }

        // Only act when there is a genuine AI review finding to fix: either the
        // triggering comment is a marked AI review comment, or one already exists.
        var triggeredByAiReview = !string.IsNullOrEmpty(automationEvent.CommentBody)
            && automationEvent.CommentBody.Contains(AiPrAutomationMarkers.ReviewComment, StringComparison.Ordinal);
        var existingReviewComments = await provider.GetReviewCommentsAsync(repository, automationEvent.PullRequestNumber, ct);
        if (!triggeredByAiReview && existingReviewComments.Count == 0)
        {
            steps.Add("No actionable AI review comments found for this comment event. Skipping.");
            return new AiPrAutomationEventResult(runId, AiPrRunStatus.Succeeded, automationEvent.Kind, "No action: no AI review comments to fix.", steps, reviewComments, validationResults);
        }

        var pullRequestDetails = await provider.GetPullRequestAsync(repository, automationEvent.PullRequestNumber, ct);
        var hydratedEvent = HydrateFromPullRequest(automationEvent, pullRequestDetails);
        var request = ToRequest(hydratedEvent);

        var repositoryPath = await workspaceService.PrepareWorkspaceAsync(runId, hydratedEvent.GitProvider, repository, ct);
        steps.Add($"Workspace prepared for fix agent at {repositoryPath}.");

        var checkoutResult = await CheckoutBranchAsync(repositoryPath, hydratedEvent.SourceBranch, ct);
        validationResults.Add(checkoutResult);
        if (!checkoutResult.Succeeded)
        {
            return new AiPrAutomationEventResult(runId, AiPrRunStatus.Failed, automationEvent.Kind, "Unable to checkout PR source branch.", steps, reviewComments, validationResults);
        }

        var providerComments = await provider.GetReviewCommentsAsync(repository, automationEvent.PullRequestNumber, ct);
        if (!string.IsNullOrWhiteSpace(hydratedEvent.CommentBody))
        {
            providerComments = providerComments
                .Concat(new[]
                {
                    new PullRequestComment(
                        "webhook-comment",
                        hydratedEvent.CommentBody,
                        "webhook",
                        hydratedEvent.CommentPath,
                        hydratedEvent.CommentLine,
                        IsResolved: false)
                })
                .ToArray();
        }

        var fix = await fixAgent.FixAsync(request, providerComments, repositoryPath, ct);
        steps.Add($"Fix agent result: {fix.Summary}");

        if (!fix.HasChanges)
        {
            return new AiPrAutomationEventResult(
                runId,
                AiPrRunStatus.RequiresHumanIntervention,
                automationEvent.Kind,
                "Fix agent did not produce code changes.",
                steps,
                reviewComments,
                validationResults);
        }

        var commands = hydratedEvent.ValidationCommands.Count > 0
            ? hydratedEvent.ValidationCommands
            : new[] { "dotnet build" };

        var results = await validationRunner.RunAsync(repositoryPath, commands, ct);
        validationResults.AddRange(results);
        if (results.Any(result => !result.Succeeded))
        {
            return new AiPrAutomationEventResult(
                runId,
                AiPrRunStatus.Failed,
                automationEvent.Kind,
                "Fix agent changed code, but validation failed.",
                steps,
                reviewComments,
                validationResults);
        }

        await provider.CommitChangesAsync(repositoryPath, $"AI PR Automation fixes review comments for PR #{automationEvent.PullRequestNumber}", ct);
        await provider.PushChangesAsync(repositoryPath, hydratedEvent.SourceBranch, ct);
        steps.Add("Fixes committed and pushed to PR source branch.");

        return new AiPrAutomationEventResult(
            runId,
            AiPrRunStatus.Succeeded,
            automationEvent.Kind,
            "Review comments fixed and pushed. Provider will trigger re-review on PR update.",
            steps,
            reviewComments,
            validationResults);
    }
    private async Task<ValidationResult> CheckoutBranchAsync(string repositoryPath, string branchName, CancellationToken ct)
    {
        var runner = new Execution.ProcessCommandRunner();
        return await runner.RunAsync(repositoryPath, $"git checkout {branchName}", ct);
    }


    private static AiPrAutomationEvent HydrateFromPullRequest(AiPrAutomationEvent automationEvent, PullRequestDetails details)
        => automationEvent with
        {
            PullRequestTitle = string.IsNullOrWhiteSpace(automationEvent.PullRequestTitle) ? details.Title : automationEvent.PullRequestTitle,
            PullRequestBody = string.IsNullOrWhiteSpace(automationEvent.PullRequestBody) ? details.Body : automationEvent.PullRequestBody,
            SourceBranch = string.IsNullOrWhiteSpace(automationEvent.SourceBranch) ? details.Ref.SourceBranch : automationEvent.SourceBranch,
            TargetBranch = string.IsNullOrWhiteSpace(automationEvent.TargetBranch) ? details.Ref.TargetBranch : automationEvent.TargetBranch
        };
    private static StartAiPrAutomationRequest ToRequest(AiPrAutomationEvent automationEvent)
        => new(
            automationEvent.RepositoryUrl,
            automationEvent.RepositoryOwner,
            automationEvent.RepositoryName,
            automationEvent.TargetBranch,
            automationEvent.PullRequestTitle,
            string.IsNullOrWhiteSpace(automationEvent.PullRequestBody)
                ? automationEvent.PullRequestTitle
                : automationEvent.PullRequestBody,
            automationEvent.GitProvider,
            automationEvent.AiProvider,
            DryRun: null,
            MaxIterations: null,
            automationEvent.ValidationCommands);

    private static GitRepositoryRef ToRepository(AiPrAutomationEvent automationEvent)
        => new(
            automationEvent.RepositoryUrl,
            automationEvent.RepositoryOwner,
            automationEvent.RepositoryName,
            automationEvent.DefaultBranch);
}
