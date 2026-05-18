using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IAiPrModelProviderFactory
{
    IAiPrModelProvider Create(AiPrModelProviderKind providerKind);
}
