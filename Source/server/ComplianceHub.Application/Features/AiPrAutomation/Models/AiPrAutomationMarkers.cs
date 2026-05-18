namespace ComplianceHub.Application.Features.AiPrAutomation.Models;

/// <summary>
/// Hidden markers embedded in AI-authored PR artifacts so the automation can
/// reliably recognise its own review comments (consistent read/write bucket,
/// loop counting, and bot-loop suppression).
/// </summary>
public static class AiPrAutomationMarkers
{
    /// <summary>Prefix added to every AI review comment body.</summary>
    public const string ReviewComment = "<!-- ai-pr-review -->";

    /// <summary>Prefix for the terminal "needs a human" notice (never re-triggers the fix agent).</summary>
    public const string HumanInterventionNotice = "<!-- ai-pr-human-needed -->";
}
