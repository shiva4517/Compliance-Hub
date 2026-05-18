namespace ComplianceHub.Application.Features.AiPrAutomation.Models;

public enum AiPrAutomationMode
{
    DryRun = 0,
    Live = 1
}

public enum GitProviderKind
{
    GitHub = 0,
    AzureDevOps = 1,
    GitLab = 2,
    Bitbucket = 3,
    SelfHosted = 4
}

public enum AiPrModelProviderKind
{
    Mock = 0,
    Anthropic = 1,
    OpenAI = 2,
    Gemini = 3
}

public enum AiPrRunStatus
{
    NotStarted = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    RequiresHumanIntervention = 4
}

public enum PullRequestReviewDecision
{
    ChangesRequested = 0,
    Approved = 1
}

public enum WorkflowRunConclusion
{
    Unknown = 0,
    Success = 1,
    Failure = 2,
    Cancelled = 3,
    TimedOut = 4
}
