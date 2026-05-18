using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using Microsoft.Extensions.Options;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Workflow;

internal sealed class AiPrWorkflowEngine(
    IGitProviderFactory gitProviderFactory,
    IAiPrModelProviderFactory aiProviderFactory,
    IRepositoryWorkspaceService workspaceService,
    ICodeGenerationAgent codingAgent,
    IPullRequestReviewerAgent reviewerAgent,
    IReviewFixAgent fixAgent,
    IValidationRunner validationRunner,
    IOptions<AiPrAutomationOptions> options) : IAiPrWorkflowEngine
{
    public async Task<AiPrAutomationRunResult> RunAsync(StartAiPrAutomationRequest request, CancellationToken ct)
    {
        var runId = Guid.NewGuid();
        var steps = new List<string>();
        var allValidationResults = new List<ValidationResult>();
        var reviewComments = new List<ReviewCommentRequest>();
        var mode = ResolveMode(request);
        var maxIterations = request.MaxIterations ?? options.Value.MaxReviewFixIterations;

        if (options.Value.AllowMerge)
        {
            throw new InvalidOperationException("AI PR automation cannot run with AllowMerge enabled. Human merge is mandatory.");
        }

        var repository = new GitRepositoryRef(
            request.RepositoryUrl,
            request.RepositoryOwner,
            request.RepositoryName,
            request.BaseBranch);

        var gitProvider = gitProviderFactory.Create(request.GitProvider);
        var aiProvider = aiProviderFactory.Create(request.AiProvider);
        var branchName = BuildBranchName(request.TaskTitle, runId);

        try
        {
            steps.Add($"Run {runId} started in {mode} mode.");

            var plan = await aiProvider.GenerateCodePlanAsync(request, ct);
            steps.Add($"Code plan generated: {plan.Summary}");

            var repositoryPath = await workspaceService.PrepareWorkspaceAsync(runId, request.GitProvider, repository, ct);
            steps.Add($"Workspace prepared at {repositoryPath}.");

            await gitProvider.CreateBranchAsync(repository, request.BaseBranch, branchName, ct);
            steps.Add($"Feature branch prepared: {branchName}.");

            var codeResult = await codingAgent.ExecuteAsync(request, plan, repositoryPath, ct);
            steps.Add($"Coding agent completed: {codeResult.Summary}");

            var changedLocalFiles = await workspaceService.GetChangedFilesAsync(repositoryPath, ct);
            if (!codeResult.HasChanges && changedLocalFiles.Count == 0 && mode == AiPrAutomationMode.Live)
            {
                return new AiPrAutomationRunResult(
                    runId,
                    AiPrRunStatus.RequiresHumanIntervention,
                    mode,
                    "No code changes were produced. The workflow stopped before creating a pull request.",
                    null,
                    steps,
                    allValidationResults,
                    reviewComments);
            }

            var validationCommands = request.ValidationCommands.Count > 0
                ? request.ValidationCommands
                : plan.ValidationCommands;
            var validationResults = await validationRunner.RunAsync(repositoryPath, validationCommands, ct);
            allValidationResults.AddRange(validationResults);
            steps.Add($"Validation completed. Passed: {validationResults.All(r => r.Succeeded)}.");

            if (validationResults.Any(r => !r.Succeeded))
            {
                var failureSummary = await aiProvider.SummarizeValidationFailureAsync(request, validationResults, ct);
                steps.Add($"Validation failure summary: {failureSummary}");

                return new AiPrAutomationRunResult(
                    runId,
                    AiPrRunStatus.Failed,
                    mode,
                    "Validation failed before pull request creation.",
                    null,
                    steps,
                    allValidationResults,
                    reviewComments);
            }

            await gitProvider.CommitChangesAsync(repositoryPath, $"AI PR Automation: {request.TaskTitle}", ct);
            await gitProvider.PushChangesAsync(repositoryPath, branchName, ct);
            steps.Add("Changes committed and pushed.");

            var pullRequest = await gitProvider.CreatePullRequestAsync(
                repository,
                branchName,
                request.BaseBranch,
                request.TaskTitle,
                BuildPullRequestBody(request, runId, mode),
                ct);
            steps.Add($"Pull request created: {pullRequest.Url}.");

            for (var iteration = 1; iteration <= maxIterations; iteration++)
            {
                var changedFiles = await gitProvider.GetChangedFilesAsync(repository, pullRequest.Number, ct);
                var diff = await gitProvider.GetDiffAsync(repository, pullRequest.Number, ct);
                var review = await reviewerAgent.ReviewAsync(request, changedFiles, diff, ct);
                steps.Add($"Review iteration {iteration}: {review.Summary}");

                if (review.Decision == PullRequestReviewDecision.Approved || review.Comments.Count == 0)
                {
                    await gitProvider.ApprovePullRequestAsync(repository, pullRequest.Number, "AI review completed. No blocking findings remain.", ct);
                    steps.Add("Pull request approved by AI reviewer.");

                    return new AiPrAutomationRunResult(
                        runId,
                        AiPrRunStatus.Succeeded,
                        mode,
                        "AI PR automation completed successfully. Pull request is ready for human merge.",
                        pullRequest.Url,
                        steps,
                        allValidationResults,
                        reviewComments);
                }

                reviewComments.AddRange(review.Comments);
                foreach (var comment in review.Comments)
                {
                    await gitProvider.AddReviewCommentAsync(repository, pullRequest.Number, comment, ct);
                }

                var providerComments = await gitProvider.GetReviewCommentsAsync(repository, pullRequest.Number, ct);
                var fixResult = await fixAgent.FixAsync(request, providerComments, repositoryPath, ct);
                steps.Add($"Fix iteration {iteration}: {fixResult.Summary}");

                if (!fixResult.HasChanges)
                {
                    return new AiPrAutomationRunResult(
                        runId,
                        AiPrRunStatus.RequiresHumanIntervention,
                        mode,
                        "Review comments remain, but the fix agent did not produce changes.",
                        pullRequest.Url,
                        steps,
                        allValidationResults,
                        reviewComments);
                }

                var fixValidationResults = await validationRunner.RunAsync(repositoryPath, validationCommands, ct);
                allValidationResults.AddRange(fixValidationResults);
                if (fixValidationResults.Any(r => !r.Succeeded))
                {
                    return new AiPrAutomationRunResult(
                        runId,
                        AiPrRunStatus.Failed,
                        mode,
                        "Validation failed after applying review fixes.",
                        pullRequest.Url,
                        steps,
                        allValidationResults,
                        reviewComments);
                }

                await gitProvider.CommitChangesAsync(repositoryPath, $"AI PR Automation fixes: {request.TaskTitle}", ct);
                await gitProvider.PushChangesAsync(repositoryPath, branchName, ct);
                steps.Add($"Fix iteration {iteration} committed and pushed.");
            }

            return new AiPrAutomationRunResult(
                runId,
                AiPrRunStatus.RequiresHumanIntervention,
                mode,
                "Maximum review/fix iterations reached. Human review is required.",
                pullRequest.Url,
                steps,
                allValidationResults,
                reviewComments);
        }
        catch (Exception ex)
        {
            steps.Add($"Workflow failed: {ex.Message}");
            return new AiPrAutomationRunResult(
                runId,
                AiPrRunStatus.Failed,
                mode,
                ex.Message,
                null,
                steps,
                allValidationResults,
                reviewComments);
        }
    }

    private AiPrAutomationMode ResolveMode(StartAiPrAutomationRequest request)
        => request.DryRun.HasValue
            ? request.DryRun.Value ? AiPrAutomationMode.DryRun : options.Value.Mode
            : options.Value.Mode;

    private static string BuildBranchName(string title, Guid runId)
    {
        var slug = new string(title
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        if (slug.Length > 48)
        {
            slug = slug[..48].Trim('-');
        }

        return $"ai-pr/{slug}-{runId:N}"[..Math.Min(80, $"ai-pr/{slug}-{runId:N}".Length)];
    }

    private static string BuildPullRequestBody(StartAiPrAutomationRequest request, Guid runId, AiPrAutomationMode mode)
        => $"""
        Automated AI PR created for:

        {request.TaskDescription}

        Run ID: {runId}
        Mode: {mode}

        This workflow never auto-merges or auto-deploys. Final merge remains human-controlled.
        """;
}
