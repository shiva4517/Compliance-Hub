using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;
using Microsoft.Extensions.Options;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Validation;

internal sealed class ValidationRunner(ProcessCommandRunner commandRunner, IOptions<AiPrAutomationOptions> options) : IValidationRunner
{
    public async Task<IReadOnlyList<ValidationResult>> RunAsync(string repositoryPath, IReadOnlyList<string> commands, CancellationToken ct)
    {
        if (options.Value.Mode == AiPrAutomationMode.DryRun)
        {
            return commands
                .DefaultIfEmpty("dry-run validation")
                .Select(command => new ValidationResult(
                    command,
                    Succeeded: true,
                    ExitCode: 0,
                    Output: "Dry-run mode: validation command was recorded but not executed.",
                    ErrorOutput: string.Empty,
                    StartedAt: DateTimeOffset.UtcNow,
                    CompletedAt: DateTimeOffset.UtcNow))
                .ToArray();
        }

        var results = new List<ValidationResult>();

        foreach (var command in commands.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var result = await commandRunner.RunAsync(repositoryPath, command, ct);
            results.Add(result);

            if (!result.Succeeded)
            {
                break;
            }
        }

        return results;
    }
}
