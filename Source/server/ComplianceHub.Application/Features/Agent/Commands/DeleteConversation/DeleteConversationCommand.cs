using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Agent.Commands.DeleteConversation;

public record DeleteConversationCommand(Guid ConversationId) : IRequest;

public class DeleteConversationCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeleteConversationCommand>
{
    public async Task Handle(DeleteConversationCommand request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
            throw new UnauthorizedException();

        var conversation = await uow.AgentConversations.Query()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
            ?? throw new NotFoundException(nameof(AgentConversation), request.ConversationId);

        if (conversation.SecurityUserId != securityUserId)
            throw new ForbiddenException();

        conversation.IsDeleted = true;
        conversation.UpdatedAt = DateTime.UtcNow;
        uow.AgentConversations.Update(conversation);
        await uow.SaveChangesAsync(ct);
    }
}
