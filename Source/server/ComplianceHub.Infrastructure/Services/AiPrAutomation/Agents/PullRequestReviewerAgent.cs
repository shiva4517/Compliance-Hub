using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Agents;

internal sealed class PullRequestReviewerAgent(IAiPrModelProviderFactory providerFactory) : IPullRequestReviewerAgent
{
    public Task<PullRequestReviewResult> ReviewAsync(StartAiPrAutomationRequest request, IReadOnlyList<ChangedFile> changedFiles, string diff, CancellationToken ct)
        => providerFactory.Create(request.AiProvider).ReviewPullRequestAsync(request, changedFiles, diff, ct);
}
