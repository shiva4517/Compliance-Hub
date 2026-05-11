using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Agent.Commands.CreateConversation;
using ComplianceHub.Application.Features.Agent.Commands.DeleteConversation;
using ComplianceHub.Application.Features.Agent.Commands.SendMessage;
using ComplianceHub.Application.Features.Agent.Dtos;
using ComplianceHub.Application.Features.Agent.Exceptions;
using ComplianceHub.Application.Features.Agent.Queries.GetConversationById;
using ComplianceHub.Application.Features.Agent.Queries.GetConversations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize(Policy = "Admin")]
public class AgentController(IMediator mediator) : ControllerBase
{
    [HttpPost("conversations")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateConversation(CancellationToken ct)
    {
        var id = await mediator.Send(new CreateConversationCommand(), ct);
        return CreatedAtAction(nameof(GetConversation), new { id },
            ApiResponse<Guid>.Ok(id, "Conversation created."));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConversationSummaryDto>>>> GetConversations(CancellationToken ct)
    {
        var result = await mediator.Send(new GetConversationsQuery(), ct);
        return Ok(ApiResponse<IReadOnlyList<ConversationSummaryDto>>.Ok(result));
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConversationDetailDto>>> GetConversation(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetConversationByIdQuery(id), ct);
        return Ok(ApiResponse<ConversationDetailDto>.Ok(result));
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<SendMessageResponse>>> SendMessage(
        Guid id,
        [FromBody] SendMessageRequest body,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new SendMessageCommand(id, body.Message), ct);
            return Ok(ApiResponse<SendMessageResponse>.Ok(result));
        }
        catch (AiProviderNotConfiguredException)
        {
            return StatusCode(412, new
            {
                success   = false,
                errorCode = "AI_PROVIDER_NOT_CONFIGURED",
                message   = "You haven't configured an AI provider yet. Please set one up before using the Agent."
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new
            {
                success   = false,
                errorCode = "AI_PROVIDER_ERROR",
                message   = ex.Message
            });
        }
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteConversation(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteConversationCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Conversation deleted."));
    }
}

public record SendMessageRequest(string Message);
