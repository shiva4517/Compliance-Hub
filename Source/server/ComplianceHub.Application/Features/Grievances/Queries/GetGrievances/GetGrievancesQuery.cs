using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Grievances.Queries.GetGrievances;

public record GrievanceListItemDto(
    Guid Id,
    string Subject,
    string Description,
    Guid CompanyId,
    string? CompanyName,
    Guid? SubscriptionId,
    string? SubscriptionName,
    string Status,
    string Priority,
    bool IsActive,
    Guid CreatedBySecurityUserId,
    string CreatedByName,
    string CreatedByRole,
    Guid RecipientSecurityUserId,
    string RecipientName,
    string RecipientRole,
    int ReplyCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime LastMessageAt,
    bool CanReply);

public record GetGrievancesQuery : IRequest<List<GrievanceListItemDto>>;

public class GetGrievancesQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetGrievancesQuery, List<GrievanceListItemDto>>
{
    public async Task<List<GrievanceListItemDto>> Handle(GetGrievancesQuery request, CancellationToken ct)
    {
        var actor = await GrievanceAccess.ResolveActorAsync(uow, currentUser, ct);

        var grievances = await uow.Grievances.Query()
            .Include(x => x.Company)
            .Include(x => x.Subscription)
            .Include(x => x.CreatedBySecurityUser)
            .Include(x => x.RecipientSecurityUser)
            .Include(x => x.Replies)
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync(ct);

        return grievances
            .Where(g => GrievanceAccess.CanView(actor, g))
            .Select(g => new GrievanceListItemDto(
                g.Id,
                g.Subject,
                g.Description,
                g.CompanyId,
                g.Company?.CompanyName,
                g.SubscriptionId,
                g.Subscription?.SubscribedNodeName,
                g.Status.ToString(),
                g.Priority.ToString(),
                g.IsActive,
                g.CreatedBySecurityUserId,
                g.CreatedBySecurityUser?.FullName ?? "Unknown User",
                g.CreatedByUserType.ToString(),
                g.RecipientSecurityUserId,
                g.RecipientSecurityUser?.FullName ?? "Unknown User",
                g.RecipientUserType.ToString(),
                g.Replies.Count,
                g.CreatedAt,
                g.UpdatedAt,
                g.LastMessageAt,
                GrievanceAccess.CanReply(actor, g)))
            .ToList();
    }
}
