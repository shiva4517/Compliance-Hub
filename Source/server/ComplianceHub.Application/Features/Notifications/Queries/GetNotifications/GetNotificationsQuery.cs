using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(
    Guid CompanyId,
    string? Search,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<NotificationLogDto>>;

public record NotificationLogDto(
    Guid Id,
    Guid CustomerId,
    string? CustomerName,
    Guid? GovernmentEntityId,
    string? GovernmentEntityName,
    Guid? PreviousRegulationId,
    string? PreviousRegulationName,
    Guid? PresentRegulationId,
    string? PresentRegulationName,
    string? RecipientEmail,
    string? RecipientName,
    bool IsNotified,
    string Status,
    int RetryCount,
    DateTime? LastRetryAt,
    DateTime CreatedAt);

public class GetNotificationsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetNotificationsQuery, PaginatedList<NotificationLogDto>>
{
    public async Task<PaginatedList<NotificationLogDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var query = uow.NotificationHistory.Query()
            .Include(n => n.Customer)
            .Where(n => n.CompanyId == request.CompanyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(n =>
                (n.Customer != null && n.Customer.CustomerName.ToLower().Contains(search)) ||
                (n.PreviousRegulationName != null && n.PreviousRegulationName.ToLower().Contains(search)) ||
                (n.PresentRegulationName != null && n.PresentRegulationName.ToLower().Contains(search)) ||
                n.Status.ToLower().Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationLogDto(
                n.Id,
                n.CustomerId,
                n.Customer != null ? n.Customer.CustomerName : null,
                n.GovernmentEntityId,
                n.GovernmentEntityName,
                n.PreviousRegulationId,
                n.PreviousRegulationName,
                n.PresentRegulationId,
                n.PresentRegulationName,
                n.RecipientEmail,
                n.RecipientName,
                n.IsNotified,
                n.Status,
                n.RetryCount,
                n.LastRetryAt,
                n.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<NotificationLogDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
