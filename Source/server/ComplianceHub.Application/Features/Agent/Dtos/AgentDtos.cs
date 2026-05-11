namespace ComplianceHub.Application.Features.Agent.Dtos;

public record ConversationSummaryDto(
    Guid Id,
    string Title,
    string Status,
    string? LastOperation,
    DateTime UpdatedAt,
    DateTime CreatedAt);

public record MessageDto(
    Guid Id,
    string Role,
    string Content,
    string? ToolName,
    DateTime CreatedAt);

public record ConversationDetailDto(
    Guid Id,
    string Title,
    string Status,
    string? LastOperation,
    string? ContextDataJson,
    IReadOnlyList<MessageDto> Messages,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SendMessageResponse(
    Guid ConversationId,
    MessageDto AssistantMessage,
    string Status,
    string? LastOperation,
    string? ContextDataJson);
