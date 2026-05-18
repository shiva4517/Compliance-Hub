namespace ComplianceHub.Application.Features.AiPrAutomation.Models;

public sealed record CodeGenerationPlan(
    string Summary,
    IReadOnlyList<string> TargetAreas,
    IReadOnlyList<string> ValidationCommands);

public sealed record CodeGenerationResult(
    string Summary,
    IReadOnlyList<string> ModifiedFiles,
    bool HasChanges);

public sealed record PullRequestReviewResult(
    PullRequestReviewDecision Decision,
    string Summary,
    IReadOnlyList<ReviewCommentRequest> Comments);

public sealed record ReviewFixResult(
    string Summary,
    IReadOnlyList<string> ModifiedFiles,
    bool HasChanges);

public sealed record ValidationResult(
    string Command,
    bool Succeeded,
    int ExitCode,
    string Output,
    string ErrorOutput,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);

public sealed record AiPrAutomationRunResult(
    Guid RunId,
    AiPrRunStatus Status,
    AiPrAutomationMode Mode,
    string Summary,
    string? PullRequestUrl,
    IReadOnlyList<string> Steps,
    IReadOnlyList<ValidationResult> ValidationResults,
    IReadOnlyList<ReviewCommentRequest> ReviewComments);
