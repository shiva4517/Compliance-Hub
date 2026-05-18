namespace ComplianceHub.Application.Features.AiPrAutomation.Models;

public sealed record StartAiPrAutomationRequest(
    string RepositoryUrl,
    string RepositoryOwner,
    string RepositoryName,
    string BaseBranch,
    string TaskTitle,
    string TaskDescription,
    GitProviderKind GitProvider,
    AiPrModelProviderKind AiProvider,
    bool? DryRun,
    int? MaxIterations,
    IReadOnlyList<string> ValidationCommands);
