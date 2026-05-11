using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Subscriptions.Queries.GetSubscriptions;

public record GetSubscriptionsQuery(Guid? CustomerId, Guid? CompanyId, int PageNumber = 1, int PageSize = 50)
    : IRequest<PaginatedList<SubscriptionDto>>;

public record SubscriptionDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid GovernmentEntityId,
    Guid? AgencyId,
    Guid? RegulationCategoryId,
    Guid? RegulationTypeId,
    Guid? RegulationSubtypeId,
    Guid? RegulationId,
    SubscribingLevel SubscribingLevel,
    string SubscribedNodeName,
    bool IsActive,
    DateTime CreatedAt);

public class GetSubscriptionsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSubscriptionsQuery, PaginatedList<SubscriptionDto>>
{
    public async Task<PaginatedList<SubscriptionDto>> Handle(GetSubscriptionsQuery request, CancellationToken ct)
    {
        var query = uow.Subscriptions.Query().Include(s => s.Customer).AsQueryable();

        if (request.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == request.CustomerId.Value);
        else if (request.CompanyId.HasValue)
            query = query.Where(s => s.Customer!.CompanyId == request.CompanyId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SubscriptionDto(
                s.Id, s.CustomerId, s.Customer!.CustomerName,
                s.GovernmentEntityId, s.AgencyId, s.RegulationCategoryId,
                s.RegulationTypeId, s.RegulationSubtypeId, s.RegulationId,
                s.SubscribingLevel, s.SubscribedNodeName, s.IsActive, s.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<SubscriptionDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
