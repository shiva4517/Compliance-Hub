using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Grievances.Queries.GetGrievanceById;

public record GrievanceReplyDto(
    Guid Id,
    Guid SenderSecurityUserId,
    string SenderName,
    string SenderRole,
    string Message,
    DateTime CreatedAt);

public record GrievanceDetailDto(
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
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime LastMessageAt,
    bool CanReply,
    List<GrievanceReplyDto> Replies);

public record GetGrievanceByIdQuery(Guid Id) : IRequest<GrievanceDetailDto>;

public class GetGrievanceByIdQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetGrievanceByIdQuery, GrievanceDetailDto>
{
    public async Task<GrievanceDetailDto> Handle(GetGrievanceByIdQuery request, CancellationToken ct)
    {
        var actor = await GrievanceAccess.ResolveActorAsync(uow, currentUser, ct);

        var grievance = await uow.Grievances.Query()
            .Include(x => x.Company)
            .Include(x => x.Subscription)
            .Include(x => x.CreatedBySecurityUser)
            .Include(x => x.RecipientSecurityUser)
            .Include(x => x.Replies)
                .ThenInclude(x => x.SenderSecurityUser)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Grievance), request.Id);

        if (!GrievanceAccess.CanView(actor, grievance))
            throw new UnauthorizedAccessException("You are not allowed to view this grievance.");

        return new GrievanceDetailDto(
            grievance.Id,
            grievance.Subject,
            grievance.Description,
            grievance.CompanyId,
            grievance.Company?.CompanyName,
            grievance.SubscriptionId,
            grievance.Subscription?.SubscribedNodeName,
            grievance.Status.ToString(),
            grievance.Priority.ToString(),
            grievance.IsActive,
            grievance.CreatedBySecurityUserId,
            grievance.CreatedBySecurityUser?.FullName ?? "Unknown User",
            grievance.CreatedByUserType.ToString(),
            grievance.RecipientSecurityUserId,
            grievance.RecipientSecurityUser?.FullName ?? "Unknown User",
            grievance.RecipientUserType.ToString(),
            grievance.CreatedAt,
            grievance.UpdatedAt,
            grievance.LastMessageAt,
            GrievanceAccess.CanReply(actor, grievance),
            grievance.Replies
                .OrderBy(x => x.CreatedAt)
                .Select(x => new GrievanceReplyDto(
                    x.Id,
                    x.SenderSecurityUserId,
                    x.SenderSecurityUser?.FullName ?? "Unknown User",
                    x.SenderUserType.ToString(),
                    x.Message,
                    x.CreatedAt))
                .ToList());
    }
}
