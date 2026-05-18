using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Ai;

internal sealed class AiPrModelProviderFactory(IEnumerable<IAiPrModelProvider> providers) : IAiPrModelProviderFactory
{
    public IAiPrModelProvider Create(AiPrModelProviderKind providerKind)
        => providers.FirstOrDefault(provider => provider.Kind == providerKind)
            ?? throw new NotSupportedException($"AI PR model provider '{providerKind}' is not registered.");
}
