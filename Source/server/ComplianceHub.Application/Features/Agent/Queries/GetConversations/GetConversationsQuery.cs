using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Agent.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Agent.Queries.GetConversations;

public record GetConversationsQuery : IRequest<IReadOnlyList<ConversationSummaryDto>>;

public class GetConversationsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationSummaryDto>>
{
    public async Task<IReadOnlyList<ConversationSummaryDto>> Handle(GetConversationsQuery request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
            throw new UnauthorizedException();

        return await uow.AgentConversations.Query()
            .Where(c => c.SecurityUserId == securityUserId)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Select(c => new ConversationSummaryDto(
                c.Id,
                c.Title,
                c.Status,
                c.LastOperation,
                c.UpdatedAt ?? c.CreatedAt,
                c.CreatedAt))
            .ToListAsync(ct);
    }
}
