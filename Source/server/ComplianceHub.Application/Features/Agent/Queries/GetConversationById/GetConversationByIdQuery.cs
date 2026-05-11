using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Agent.Dtos;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Agent.Queries.GetConversationById;

public record GetConversationByIdQuery(Guid ConversationId) : IRequest<ConversationDetailDto>;

public class GetConversationByIdQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetConversationByIdQuery, ConversationDetailDto>
{
    public async Task<ConversationDetailDto> Handle(GetConversationByIdQuery request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
            throw new UnauthorizedException();

        var conversation = await uow.AgentConversations.Query()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
            ?? throw new NotFoundException(nameof(AgentConversation), request.ConversationId);

        if (conversation.SecurityUserId != securityUserId)
            throw new ForbiddenException();

        var messages = conversation.Messages
            .Where(m => m.Role is "user" or "assistant")
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(m.Id, m.Role, m.Content, m.ToolName, m.CreatedAt))
            .ToList();

        return new ConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.Status,
            conversation.LastOperation,
            conversation.ContextDataJson,
            messages,
            conversation.CreatedAt,
            conversation.UpdatedAt ?? conversation.CreatedAt);
    }
}
