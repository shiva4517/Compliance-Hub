using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;
using Microsoft.Extensions.Options;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Workspace;

internal sealed class RepositoryWorkspaceService(
    IGitProviderFactory gitProviderFactory,
    IOptions<AiPrAutomationOptions> options,
    ProcessCommandRunner commandRunner) : IRepositoryWorkspaceService
{
    public async Task<string> PrepareWorkspaceAsync(Guid runId, GitProviderKind providerKind, GitRepositoryRef repository, CancellationToken ct)
    {
        var root = Path.GetFullPath(options.Value.WorkspaceRoot);
        Directory.CreateDirectory(root);

        var runPath = Path.Combine(root, runId.ToString("N"));
        if (Directory.Exists(runPath))
        {
            Directory.Delete(runPath, recursive: true);
        }

        var provider = gitProviderFactory.Create(providerKind);
        await provider.CloneRepositoryAsync(repository, runPath, ct);

        return runPath;
    }

    public async Task<IReadOnlyList<string>> GetChangedFilesAsync(string repositoryPath, CancellationToken ct)
    {
        var result = await commandRunner.RunAsync(repositoryPath, "git status --short", ct);
        if (!result.Succeeded)
        {
            return Array.Empty<string>();
        }

        return result.Output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Length > 3 ? line[3..].Trim() : line.Trim())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
