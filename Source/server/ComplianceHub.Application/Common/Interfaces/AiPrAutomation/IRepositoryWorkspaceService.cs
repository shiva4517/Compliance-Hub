using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IRepositoryWorkspaceService
{
    Task<string> PrepareWorkspaceAsync(Guid runId, GitProviderKind providerKind, GitRepositoryRef repository, CancellationToken ct);
    Task<IReadOnlyList<string>> GetChangedFilesAsync(string repositoryPath, CancellationToken ct);
}
