using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IAiPrModelProvider
{
    AiPrModelProviderKind Kind { get; }

    Task<CodeGenerationPlan> GenerateCodePlanAsync(StartAiPrAutomationRequest request, CancellationToken ct);
    Task<CodeGenerationResult> GenerateCodeChangesAsync(StartAiPrAutomationRequest request, CodeGenerationPlan plan, string repositoryPath, CancellationToken ct);
    Task<PullRequestReviewResult> ReviewPullRequestAsync(StartAiPrAutomationRequest request, IReadOnlyList<ChangedFile> changedFiles, string diff, CancellationToken ct);
    Task<ReviewFixResult> FixReviewCommentsAsync(StartAiPrAutomationRequest request, IReadOnlyList<PullRequestComment> comments, string repositoryPath, CancellationToken ct);
    Task<string> SummarizeValidationFailureAsync(StartAiPrAutomationRequest request, IReadOnlyList<ValidationResult> validationResults, CancellationToken ct);
}
