using System.Text.Json;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Agent.Dtos;
using ComplianceHub.Application.Features.Agent.Models;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Agent.Commands.SendMessage;

public record SendMessageCommand(Guid ConversationId, string UserMessage) : IRequest<SendMessageResponse>;

public class SendMessageCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    ILlmClientFactory llmClientFactory,
    IAgentToolRouter toolRouter)
    : IRequestHandler<SendMessageCommand, SendMessageResponse>
{
    private const int MaxToolLoopIterations = 8;

    public async Task<SendMessageResponse> Handle(SendMessageCommand request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
            throw new UnauthorizedException();

        var conversation = await uow.AgentConversations.Query()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
            ?? throw new NotFoundException(nameof(AgentConversation), request.ConversationId);

        if (conversation.SecurityUserId != securityUserId)
            throw new ForbiddenException();

        // Throws AiProviderNotConfiguredException if user has no connection row
        var llmClient = await llmClientFactory.CreateForUserAsync(securityUserId, ct);

        // Persist user message
        var userMsg = new AgentMessage
        {
            ConversationId = conversation.Id,
            Role           = "user",
            Content        = request.UserMessage,
            CreatedAt      = DateTime.UtcNow,
            CreatedBy      = currentUser.Email
        };
        await uow.AgentMessages.AddAsync(userMsg, ct);

        // Auto-title from first user message
        if (conversation.Title == "New Conversation")
        {
            var t = request.UserMessage.Trim();
            conversation.Title = t.Length > 50 ? t[..50] + "…" : t;
        }

        await uow.SaveChangesAsync(ct);

        // Build history from DB — only user/assistant messages ever stored
        var storedMessages = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new LlmMessage(m.Role, m.Content))
            .ToList();

        var role     = currentUser.Role ?? "SuperAdmin";
        var toolDefs = toolRouter.GetToolDefinitions(role);

        // Ephemeral in-memory list for tool loop within this request.
        // Starts with DB history. Tool call/result messages are appended
        // here only — they are never written to the database.
        var ephemeralHistory = new List<LlmMessage>(storedMessages);

        string? lastOperation   = conversation.LastOperation;
        string? contextDataJson = conversation.ContextDataJson;
        string  finalContent    = "(no response)";

        for (var iteration = 0; iteration < MaxToolLoopIterations; iteration++)
        {
            var llmRequest = new LlmRequest(
                SystemPrompt: toolRouter.GetSystemPrompt(role),
                Messages:     ephemeralHistory,
                Tools:        toolDefs,
                MaxTokens:    20048,
                Temperature:  0.3);

            var llmResponse = await llmClient.SendAsync(llmRequest, ct);

            if (llmResponse.ToolCalls is { Count: > 0 })
            {
                foreach (var toolCall in llmResponse.ToolCalls)
                {
                    var toolResult = await toolRouter.InvokeToolAsync(
                        toolCall.Name, toolCall.ArgumentsJson, role, ct);

                    // Keep tool interaction in-memory only — never written to DB.
                    // Content = ArgumentsJson so provider clients can reconstruct the native tool_use block.
                    ephemeralHistory.Add(new LlmMessage("assistant",
                        toolCall.ArgumentsJson, toolCall.Name, toolCall.Id));
                    ephemeralHistory.Add(new LlmMessage("tool",
                        toolResult, toolCall.Name, toolCall.Id));

                    (lastOperation, contextDataJson) = UpdateOperationState(
                        toolCall.Name, toolCall.ArgumentsJson, toolResult,
                        lastOperation, contextDataJson);
                }
                continue;
            }

            finalContent = llmResponse.Content ?? "(no response)";
            break;
        }

        // Persist only the final assistant text — what the user actually sees
        var assistantMsg = new AgentMessage
        {
            ConversationId = conversation.Id,
            Role           = "assistant",
            Content        = finalContent,
            CreatedAt      = DateTime.UtcNow,
            CreatedBy      = currentUser.Email
        };
        await uow.AgentMessages.AddAsync(assistantMsg, ct);

        conversation.LastOperation   = lastOperation;
        conversation.ContextDataJson = contextDataJson;
        conversation.UpdatedAt       = DateTime.UtcNow;
        conversation.UpdatedBy       = currentUser.Email;
        uow.AgentConversations.Update(conversation);

        await uow.SaveChangesAsync(ct);

        return new SendMessageResponse(
            conversation.Id,
            new MessageDto(assistantMsg.Id, "assistant", finalContent, null, assistantMsg.CreatedAt),
            conversation.Status,
            lastOperation,
            contextDataJson);
    }

    private static (string? operation, string? contextJson) UpdateOperationState(
        string toolName, string argsJson, string resultJson,
        string? currentOperation, string? currentContext)
    {
        return toolName switch
        {
            "create_company"       => ("create_company", MergeContext(argsJson, resultJson)),
            "update_company"       => ("update_company", MergeContext(argsJson, resultJson)),
            "list_companies"       => ("list_companies", resultJson),
            "get_company"          => ("get_company",    resultJson),
            "preview_company_data" => (currentOperation, resultJson),
            _                      => (currentOperation, currentContext)
        };
    }

    private static string? MergeContext(string argsJson, string resultJson)
    {
        try
        {
            return JsonSerializer.Serialize(new
            {
                args   = JsonSerializer.Deserialize<object>(argsJson),
                result = JsonSerializer.Deserialize<object>(resultJson)
            });
        }
        catch { return resultJson; }
    }

}
