using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Customers.Queries.GetCustomerSubscriptions;

public record GetCustomerSubscriptionsQuery(Guid CustomerId, Guid CompanyId)
    : IRequest<List<CustomerSubscriptionDto>>;

public record CustomerSubscriptionDto(
    Guid Id,
    SubscribingLevel SubscribingLevel,
    string SubscribedNodeName,
    Guid GovernmentEntityId,
    string GovernmentEntityName,
    Guid? AgencyId,
    string? AgencyName,
    Guid? RegulationCategoryId,
    string? RegulationCategoryName,
    Guid? RegulationTypeId,
    string? RegulationTypeName,
    Guid? RegulationSubtypeId,
    string? RegulationSubtypeName,
    bool IsActive,
    DateTime CreatedAt);

public class GetCustomerSubscriptionsQueryHandler(IUnitOfWork uow, IRegulationsUnitOfWork regulationsUow)
    : IRequestHandler<GetCustomerSubscriptionsQuery, List<CustomerSubscriptionDto>>
{
    public async Task<List<CustomerSubscriptionDto>> Handle(GetCustomerSubscriptionsQuery request, CancellationToken ct)
    {
        var subs = await uow.Subscriptions.Query()
            .Include(s => s.Customer)
            .Where(s => s.CustomerId == request.CustomerId
                     && s.Customer!.CompanyId == request.CompanyId
                     && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        if (subs.Count == 0) return [];

        var govEntityIds = subs.Select(s => s.GovernmentEntityId).Distinct().ToList();
        var agencyIds = subs.Where(s => s.AgencyId.HasValue).Select(s => s.AgencyId!.Value).Distinct().ToList();
        var categoryIds = subs.Where(s => s.RegulationCategoryId.HasValue).Select(s => s.RegulationCategoryId!.Value).Distinct().ToList();
        var typeIds = subs.Where(s => s.RegulationTypeId.HasValue).Select(s => s.RegulationTypeId!.Value).Distinct().ToList();
        var subtypeIds = subs.Where(s => s.RegulationSubtypeId.HasValue).Select(s => s.RegulationSubtypeId!.Value).Distinct().ToList();

        var govEntities = await regulationsUow.GovernmentEntities.Query()
            .Where(e => govEntityIds.Contains(e.Id))
            .Select(e => new { e.Id, Name = e.TitleName })
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        var agencies = agencyIds.Count > 0
            ? await regulationsUow.Agencies.Query()
                .Where(a => agencyIds.Contains(a.Id))
                .Select(a => new { a.Id, Name = a.AgencyName })
                .ToDictionaryAsync(a => a.Id, a => a.Name, ct)
            : new Dictionary<Guid, string>();

        var categories = categoryIds.Count > 0
            ? await regulationsUow.RegulationCategories.Query()
                .Where(c => categoryIds.Contains(c.Id))
                .Select(c => new { c.Id, Name = c.SubchapterName })
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct)
            : new Dictionary<Guid, string>();

        var types = typeIds.Count > 0
            ? await regulationsUow.RegulationTypes.Query()
                .Where(t => typeIds.Contains(t.Id))
                .Select(t => new { t.Id, Name = t.PartName })
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct)
            : new Dictionary<Guid, string>();

        var subtypes = subtypeIds.Count > 0
            ? await regulationsUow.RegulationSubtypes.Query()
                .Where(s => subtypeIds.Contains(s.Id))
                .Select(s => new { s.Id, Name = s.SubpartName })
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        return subs.Select(s => new CustomerSubscriptionDto(
            s.Id,
            s.SubscribingLevel,
            s.SubscribedNodeName,
            s.GovernmentEntityId,
            govEntities.GetValueOrDefault(s.GovernmentEntityId, "—"),
            s.AgencyId,
            s.AgencyId.HasValue ? agencies.GetValueOrDefault(s.AgencyId.Value) : null,
            s.RegulationCategoryId,
            s.RegulationCategoryId.HasValue ? categories.GetValueOrDefault(s.RegulationCategoryId.Value) : null,
            s.RegulationTypeId,
            s.RegulationTypeId.HasValue ? types.GetValueOrDefault(s.RegulationTypeId.Value) : null,
            s.RegulationSubtypeId,
            s.RegulationSubtypeId.HasValue ? subtypes.GetValueOrDefault(s.RegulationSubtypeId.Value) : null,
            s.IsActive,
            s.CreatedAt
        )).ToList();
    }
}
