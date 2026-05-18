using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Agents;

internal sealed class ReviewFixAgent(IAiPrModelProviderFactory providerFactory) : IReviewFixAgent
{
    public Task<ReviewFixResult> FixAsync(StartAiPrAutomationRequest request, IReadOnlyList<PullRequestComment> comments, string repositoryPath, CancellationToken ct)
        => providerFactory.Create(request.AiProvider).FixReviewCommentsAsync(request, comments, repositoryPath, ct);
}
