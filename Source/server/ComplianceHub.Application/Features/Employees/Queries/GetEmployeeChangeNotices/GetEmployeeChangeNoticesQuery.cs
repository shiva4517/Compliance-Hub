using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetEmployeeChangeNotices;

public record GetEmployeeChangeNoticesQuery(
    Guid EmployeeId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Search,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<EmployeeChangeNoticeDto>>;

public record EmployeeChangeNoticeDto(
    Guid Id,
    Guid RegulationId,
    Guid GovernmentEntityId,
    string GovernmentEntityName,
    string AgencyName,
    string RegulationCategoryName,
    string RegulationTypeName,
    string? RegulationSubtypeName,
    string SectionNumber,
    string SectionTitle,
    int ArchivedVersion,
    int NewVersion,
    DateTime ChangedAt);

public class GetEmployeeChangeNoticesQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetEmployeeChangeNoticesQuery, PaginatedList<EmployeeChangeNoticeDto>>
{
    public async Task<PaginatedList<EmployeeChangeNoticeDto>> Handle(
        GetEmployeeChangeNoticesQuery request, CancellationToken ct)
    {
        var assignments = await uow.EmployeeAssignedWorks.Query()
            .Include(a => a.Subscription)
            .Where(a => a.EmployeeId == request.EmployeeId && a.IsActive)
            .Select(a => a.Subscription!)
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return new PaginatedList<EmployeeChangeNoticeDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

        // Collect matching IDs per SubscribingLevel
        var entityIds = assignments
            .Where(s => s.SubscribingLevel == SubscribingLevel.Entity)
            .Select(s => s.GovernmentEntityId).ToHashSet();

        var agencyIds = assignments
            .Where(s => s.SubscribingLevel == SubscribingLevel.Agency && s.AgencyId.HasValue)
            .Select(s => s.AgencyId!.Value).ToHashSet();

        var categoryIds = assignments
            .Where(s => s.SubscribingLevel == SubscribingLevel.Category && s.RegulationCategoryId.HasValue)
            .Select(s => s.RegulationCategoryId!.Value).ToHashSet();

        var typeIds = assignments
            .Where(s => s.SubscribingLevel == SubscribingLevel.Type && s.RegulationTypeId.HasValue)
            .Select(s => s.RegulationTypeId!.Value).ToHashSet();

        var subtypeIds = assignments
            .Where(s => s.SubscribingLevel == SubscribingLevel.SubType && s.RegulationSubtypeId.HasValue)
            .Select(s => s.RegulationSubtypeId!.Value).ToHashSet();

        var regulationIds = assignments
            .Where(s => s.SubscribingLevel == SubscribingLevel.Regulation && s.RegulationId.HasValue)
            .Select(s => s.RegulationId!.Value).ToHashSet();

        var query = uow.ChangeNotices.Query().Where(c =>
            (entityIds.Count > 0 && entityIds.Contains(c.GovernmentEntityId)) ||
            (agencyIds.Count > 0 && c.AgencyId.HasValue && agencyIds.Contains(c.AgencyId!.Value)) ||
            (categoryIds.Count > 0 && c.RegulationCategoryId.HasValue && categoryIds.Contains(c.RegulationCategoryId!.Value)) ||
            (typeIds.Count > 0 && c.RegulationTypeId.HasValue && typeIds.Contains(c.RegulationTypeId!.Value)) ||
            (subtypeIds.Count > 0 && c.RegulationSubtypeId.HasValue && subtypeIds.Contains(c.RegulationSubtypeId!.Value)) ||
            (regulationIds.Count > 0 && regulationIds.Contains(c.RegulationId)));

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(c => c.ChangedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(c => c.ChangedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.GovernmentEntityName.ToLower().Contains(s) ||
                c.AgencyName.ToLower().Contains(s) ||
                c.RegulationCategoryName.ToLower().Contains(s) ||
                c.RegulationTypeName.ToLower().Contains(s) ||
                c.SectionNumber.ToLower().Contains(s) ||
                c.SectionTitle.ToLower().Contains(s) ||
                (c.RegulationSubtypeName != null && c.RegulationSubtypeName.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.ChangedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new EmployeeChangeNoticeDto(
                c.Id,
                c.RegulationId,
                c.GovernmentEntityId,
                c.GovernmentEntityName,
                c.AgencyName,
                c.RegulationCategoryName,
                c.RegulationTypeName,
                c.RegulationSubtypeName,
                c.SectionNumber,
                c.SectionTitle,
                c.ArchivedVersion,
                c.NewVersion,
                c.ChangedAt))
            .ToListAsync(ct);

        return new PaginatedList<EmployeeChangeNoticeDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
