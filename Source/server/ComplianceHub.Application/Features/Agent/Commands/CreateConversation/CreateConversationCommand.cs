using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;

namespace ComplianceHub.Application.Features.Agent.Commands.CreateConversation;

public record CreateConversationCommand : IRequest<Guid>;

public class CreateConversationCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateConversationCommand, Guid>
{
    public async Task<Guid> Handle(CreateConversationCommand request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
            throw new UnauthorizedException();

        var conversation = new AgentConversation
        {
            SecurityUserId = securityUserId,
            Title          = "New Conversation",
            Status         = "in_progress",
            CreatedAt      = DateTime.UtcNow,
            CreatedBy      = currentUser.Email
        };

        await uow.AgentConversations.AddAsync(conversation, ct);
        await uow.SaveChangesAsync(ct);

        return conversation.Id;
    }
}
