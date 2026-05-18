using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Ai;

internal sealed class MockAiPrModelProvider : IAiPrModelProvider
{
    public AiPrModelProviderKind Kind => AiPrModelProviderKind.Mock;

    public Task<CodeGenerationPlan> GenerateCodePlanAsync(StartAiPrAutomationRequest request, CancellationToken ct)
    {
        var commands = request.ValidationCommands.Count > 0
            ? request.ValidationCommands
            : new[] { "dotnet build" };

        return Task.FromResult(new CodeGenerationPlan(
            $"Dry-run plan for: {request.TaskTitle}",
            new[] { "Repository analysis", "Code generation", "Validation", "Pull request review" },
            commands));
    }

    public Task<CodeGenerationResult> GenerateCodeChangesAsync(StartAiPrAutomationRequest request, CodeGenerationPlan plan, string repositoryPath, CancellationToken ct)
        => Task.FromResult(new CodeGenerationResult(
            "Dry-run mode: no code files were modified.",
            Array.Empty<string>(),
            HasChanges: false));

    public Task<PullRequestReviewResult> ReviewPullRequestAsync(StartAiPrAutomationRequest request, IReadOnlyList<ChangedFile> changedFiles, string diff, CancellationToken ct)
        => Task.FromResult(new PullRequestReviewResult(
            PullRequestReviewDecision.Approved,
            "Dry-run review completed with no findings.",
            Array.Empty<ReviewCommentRequest>()));

    public Task<ReviewFixResult> FixReviewCommentsAsync(StartAiPrAutomationRequest request, IReadOnlyList<PullRequestComment> comments, string repositoryPath, CancellationToken ct)
        => Task.FromResult(new ReviewFixResult(
            "Dry-run mode: no review fixes were applied.",
            Array.Empty<string>(),
            HasChanges: false));

    public Task<string> SummarizeValidationFailureAsync(StartAiPrAutomationRequest request, IReadOnlyList<ValidationResult> validationResults, CancellationToken ct)
        => Task.FromResult("Validation failed during dry-run workflow. Review command output for details.");
}
