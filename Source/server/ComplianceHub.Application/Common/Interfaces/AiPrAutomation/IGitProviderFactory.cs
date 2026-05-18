using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IGitProviderFactory
{
    IGitProvider Create(GitProviderKind providerKind);
}
