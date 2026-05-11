using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetEmployeeAssignedWork;

public record GetEmployeeAssignedWorkQuery(Guid EmployeeId) : IRequest<List<AssignedWorkDto>>;

public record AssignedWorkDto(
    Guid Id,
    Guid EmployeeId,
    Guid SubscriptionId,
    Guid CustomerId,
    string CustomerName,
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
    DateTime AssignedAt,
    string AssignedBy,
    DateTime CreatedAt,
    string? CreatedBy);

public class GetEmployeeAssignedWorkQueryHandler(IUnitOfWork uow, IRegulationsUnitOfWork regulationsUow)
    : IRequestHandler<GetEmployeeAssignedWorkQuery, List<AssignedWorkDto>>
{
    public async Task<List<AssignedWorkDto>> Handle(GetEmployeeAssignedWorkQuery request, CancellationToken ct)
    {
        var assignments = await uow.EmployeeAssignedWorks.Query()
            .Include(a => a.Subscription)
            .Include(a => a.Customer)
            .Where(a => a.EmployeeId == request.EmployeeId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct);

        if (assignments.Count == 0) return [];

        var govEntityIds = assignments.Select(a => a.Subscription!.GovernmentEntityId).Distinct().ToList();
        var agencyIds = assignments.Where(a => a.Subscription!.AgencyId.HasValue).Select(a => a.Subscription!.AgencyId!.Value).Distinct().ToList();
        var categoryIds = assignments.Where(a => a.Subscription!.RegulationCategoryId.HasValue).Select(a => a.Subscription!.RegulationCategoryId!.Value).Distinct().ToList();
        var typeIds = assignments.Where(a => a.Subscription!.RegulationTypeId.HasValue).Select(a => a.Subscription!.RegulationTypeId!.Value).Distinct().ToList();
        var subtypeIds = assignments.Where(a => a.Subscription!.RegulationSubtypeId.HasValue).Select(a => a.Subscription!.RegulationSubtypeId!.Value).Distinct().ToList();


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

        return assignments.Select(a =>
        {
            var sub = a.Subscription!;
            return new AssignedWorkDto(
                a.Id,
                a.EmployeeId,
                a.SubscriptionId,
                a.CustomerId,
                a.Customer!.CustomerName,
                sub.SubscribingLevel,
                sub.SubscribedNodeName,
                sub.GovernmentEntityId,
                govEntities.GetValueOrDefault(sub.GovernmentEntityId, "—"),
                sub.AgencyId,
                sub.AgencyId.HasValue ? agencies.GetValueOrDefault(sub.AgencyId.Value) : null,
                sub.RegulationCategoryId,
                sub.RegulationCategoryId.HasValue ? categories.GetValueOrDefault(sub.RegulationCategoryId.Value) : null,
                sub.RegulationTypeId,
                sub.RegulationTypeId.HasValue ? types.GetValueOrDefault(sub.RegulationTypeId.Value) : null,
                sub.RegulationSubtypeId,
                sub.RegulationSubtypeId.HasValue ? subtypes.GetValueOrDefault(sub.RegulationSubtypeId.Value) : null,
                a.IsActive,
                a.AssignedAt,
                a.AssignedBy,
                a.CreatedAt,
                a.CreatedBy);
        }).ToList();
    }
}
