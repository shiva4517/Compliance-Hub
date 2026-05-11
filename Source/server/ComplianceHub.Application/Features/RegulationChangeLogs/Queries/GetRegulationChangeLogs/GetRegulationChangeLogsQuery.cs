using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.RegulationChangeLogs.Queries.GetRegulationChangeLogs;

public record GetRegulationChangeLogsQuery(
    string? Search,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string SortBy = "ChangedAt",
    string SortDir = "desc",
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<RegulationChangeLogDto>>;

public record RegulationChangeLogDto(
    Guid Id,
    Guid GovernmentEntityId,
    string GovernmentEntityName,
    Guid? AgencyId,
    string AgencyName,
    Guid? RegulationCategoryId,
    string RegulationCategoryName,
    Guid? RegulationTypeId,
    string RegulationTypeName,
    Guid? RegulationSubtypeId,
    string? RegulationSubtypeName,
    string SectionNumber,
    string SectionTitle,
    int ArchivedVersion,
    int NewVersion,
    DateTime ChangedAt);

public class GetRegulationChangeLogsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetRegulationChangeLogsQuery, PaginatedList<RegulationChangeLogDto>>
{
    public async Task<PaginatedList<RegulationChangeLogDto>> Handle(GetRegulationChangeLogsQuery request, CancellationToken ct)
    {
        var query = uow.ChangeNotices.Query().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.GovernmentEntityName.ToLower().Contains(s) ||
                r.AgencyName.ToLower().Contains(s) ||
                r.RegulationCategoryName.ToLower().Contains(s) ||
                r.RegulationTypeName.ToLower().Contains(s) ||
                (r.RegulationSubtypeName != null && r.RegulationSubtypeName.ToLower().Contains(s)) ||
                r.SectionNumber.ToLower().Contains(s) ||
                r.SectionTitle.ToLower().Contains(s));
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(r => r.ChangedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(r => r.ChangedAt <= to);
        }

        query = (request.SortBy?.ToLower(), request.SortDir?.ToLower()) switch
        {
            ("changedat", "asc") => query.OrderBy(r => r.ChangedAt),
            ("sectionnumber", "asc") => query.OrderBy(r => r.SectionNumber),
            ("sectionnumber", _) => query.OrderByDescending(r => r.SectionNumber),
            _ => query.OrderByDescending(r => r.ChangedAt),
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RegulationChangeLogDto(
                r.Id,
                r.GovernmentEntityId, r.GovernmentEntityName,
                r.AgencyId, r.AgencyName,
                r.RegulationCategoryId, r.RegulationCategoryName,
                r.RegulationTypeId, r.RegulationTypeName,
                r.RegulationSubtypeId, r.RegulationSubtypeName,
                r.SectionNumber, r.SectionTitle,
                r.ArchivedVersion, r.NewVersion,
                r.ChangedAt))
            .ToListAsync(ct);

        return new PaginatedList<RegulationChangeLogDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
