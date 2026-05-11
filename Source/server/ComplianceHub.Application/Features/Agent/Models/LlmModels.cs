namespace ComplianceHub.Application.Features.Agent.Models;

public record LlmMessage(string Role, string Content, string? ToolName = null, string? ToolCallId = null);

public record LlmToolDefinition(
    string Name,
    string Description,
    object InputSchema);

public record LlmToolCall(string Id, string Name, string ArgumentsJson);

public record LlmRequest(
    string SystemPrompt,
    IReadOnlyList<LlmMessage> Messages,
    IReadOnlyList<LlmToolDefinition>? Tools = null,
    int MaxTokens = 2048,
    double Temperature = 0.3);

public record LlmResponse(
    string? Content,
    IReadOnlyList<LlmToolCall>? ToolCalls,
    string StopReason);
