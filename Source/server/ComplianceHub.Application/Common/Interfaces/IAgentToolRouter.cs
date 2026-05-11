using ComplianceHub.Application.Features.Agent.Models;

namespace ComplianceHub.Application.Common.Interfaces;

public interface IAgentToolRouter
{
    IReadOnlyList<LlmToolDefinition> GetToolDefinitions(string role);
    string GetSystemPrompt(string role);
    Task<string> InvokeToolAsync(string toolName, string argumentsJson, string role, CancellationToken ct);
}
