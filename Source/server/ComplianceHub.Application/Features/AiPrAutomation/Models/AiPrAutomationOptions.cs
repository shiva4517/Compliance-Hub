namespace ComplianceHub.Application.Features.AiPrAutomation.Models;

public sealed class AiPrAutomationOptions
{
    public const string SectionName = "AiPrAutomation";

    public AiPrAutomationMode Mode { get; set; } = AiPrAutomationMode.DryRun;
    public GitProviderKind GitProvider { get; set; } = GitProviderKind.GitHub;
    public AiPrModelProviderKind AiProvider { get; set; } = AiPrModelProviderKind.Mock;
    public int MaxReviewFixIterations { get; set; } = 6;
    public string WorkspaceRoot { get; set; } = "workspaces/ai-pr";
    public bool AllowApproval { get; set; } = true;
    public bool AllowMerge { get; set; } = false;
}
