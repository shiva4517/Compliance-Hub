using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface ICodeGenerationAgent
{
    Task<CodeGenerationResult> ExecuteAsync(StartAiPrAutomationRequest request, CodeGenerationPlan plan, string repositoryPath, CancellationToken ct);
}

public interface IPullRequestReviewerAgent
{
    Task<PullRequestReviewResult> ReviewAsync(StartAiPrAutomationRequest request, IReadOnlyList<ChangedFile> changedFiles, string diff, CancellationToken ct);
}

public interface IReviewFixAgent
{
    Task<ReviewFixResult> FixAsync(StartAiPrAutomationRequest request, IReadOnlyList<PullRequestComment> comments, string repositoryPath, CancellationToken ct);
}
