using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.ChangeNotices.Queries.GetNotificationHistory;

public record GetNotificationHistoryQuery(
    Guid? CustomerId,
    Guid? CompanyId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<NotificationHistoryDto>>;

public record NotificationHistoryDto(
    Guid Id,
    Guid SubscriptionId,
    Guid CustomerId,
    string? CustomerName,
    string? RecipientEmail,
    string? Subject,
    string? GovernmentEntityName,
    string? PresentRegulationName,
    string Status,
    DateTime CreatedAt);

public class GetNotificationHistoryQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetNotificationHistoryQuery, PaginatedList<NotificationHistoryDto>>
{
    public async Task<PaginatedList<NotificationHistoryDto>> Handle(GetNotificationHistoryQuery request, CancellationToken ct)
    {
        var query = uow.NotificationHistory.Query()
            .Include(n => n.Customer)
            .AsQueryable();

        if (request.CompanyId.HasValue)
            query = query.Where(n => n.CompanyId == request.CompanyId.Value);

        if (request.CustomerId.HasValue)
            query = query.Where(n => n.CustomerId == request.CustomerId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationHistoryDto(
                n.Id,
                n.SubscriptionId,
                n.CustomerId,
                n.Customer != null ? n.Customer.CustomerName : null,
                n.RecipientEmail,
                n.Subject,
                n.GovernmentEntityName,
                n.PresentRegulationName,
                n.Status,
                n.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<NotificationHistoryDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
