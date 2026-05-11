using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetGovernmentEntities;

public record GetGovernmentEntitiesQuery(
    string? Search,
    bool? IsSyncEnabled,
    bool? IsImported,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<GovernmentEntityDto>>;

public record GovernmentEntityDto(
    Guid Id,
    int TitleNumber,
    string TitleName,
    string? Source,
    bool IsSyncEnabled,
    bool IsImported,
    DateOnly? LastAmendedDate,
    DateTime? LastSyncedDate,
    DateTime CreatedAt);

public class GetGovernmentEntitiesQueryHandler(
    IRegulationsUnitOfWork uow,
    ILogger<GetGovernmentEntitiesQueryHandler> logger)
    : IRequestHandler<GetGovernmentEntitiesQuery, PaginatedList<GovernmentEntityDto>>
{
    public async Task<PaginatedList<GovernmentEntityDto>> Handle(GetGovernmentEntitiesQuery request, CancellationToken ct)
    {
        logger.LogInformation(
            "GetGovernmentEntities request: Search={Search} IsSyncEnabled={IsSyncEnabled} IsImported={IsImported} Page={Page} Size={Size}",
            request.Search, request.IsSyncEnabled, request.IsImported, request.PageNumber, request.PageSize);

        var query = uow.GovernmentEntities.Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(e => e.TitleName.ToLower().Contains(s) || e.TitleNumber.ToString().Contains(s));
        }

        if (request.IsSyncEnabled.HasValue)
        {
            var v = request.IsSyncEnabled.Value;
            query = query.Where(e => e.IsSyncEnabled == v);
        }

        if (request.IsImported.HasValue)
        {
            var v = request.IsImported.Value;
            query = query.Where(e => e.IsImported == v);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.TitleNumber)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new GovernmentEntityDto(e.Id, e.TitleNumber, e.TitleName, e.Source,
                e.IsSyncEnabled, e.IsImported, e.LastAmendedDate, e.LastSyncedDate, e.CreatedAt))
            .ToListAsync(ct);

        logger.LogInformation(
            "GetGovernmentEntities result: filtered TotalCount={Total}, returned ItemCount={Returned} (IsImported filter={IsImported})",
            total, items.Count, request.IsImported);

        return new PaginatedList<GovernmentEntityDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
