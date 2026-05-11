using ComplianceHub.Application.Features.Agent.Models;

namespace ComplianceHub.Application.Common.Interfaces;

public interface ISubscriptionMcpService
{
    IReadOnlyList<LlmToolDefinition> GetToolDefinitions();
    Task<string> InvokeToolAsync(string toolName, string argumentsJson, CancellationToken ct);
}
