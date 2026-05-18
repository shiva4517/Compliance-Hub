using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Git;

internal sealed class GitProviderFactory(IEnumerable<IGitProvider> providers) : IGitProviderFactory
{
    public IGitProvider Create(GitProviderKind providerKind)
        => providers.FirstOrDefault(provider => provider.Kind == providerKind)
            ?? throw new NotSupportedException($"Git provider '{providerKind}' is not registered.");
}
