using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IValidationRunner
{
    Task<IReadOnlyList<ValidationResult>> RunAsync(string repositoryPath, IReadOnlyList<string> commands, CancellationToken ct);
}
