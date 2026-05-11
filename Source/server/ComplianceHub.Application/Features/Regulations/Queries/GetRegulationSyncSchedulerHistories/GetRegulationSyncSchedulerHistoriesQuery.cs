using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetRegulationSyncSchedulerHistories;

public record GetRegulationSyncSchedulerHistoriesQuery(
    string SortBy = "CompletedAt",
    string SortDir = "desc",
    int PageNumber = 1,
    int PageSize = 0) : IRequest<PaginatedList<RegulationSyncSchedulerHistoryDto>>;

public record RegulationSyncSchedulerHistoryDto(
    Guid Id,
    Guid GovernmentEntityId,
    string GovernmentEntityName,
    int TitleNumber,
    string OperationType,
    string Status,
    string TriggerSource,
    int ImportedRecordsCount,
    int ChangedRecordsCount,
    int DeactivatedRecordsCount,
    int OutboxEventsQueuedCount,
    string? Details,
    DateTime StartedAt,
    DateTime CompletedAt);

public class GetRegulationSyncSchedulerHistoriesQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetRegulationSyncSchedulerHistoriesQuery, PaginatedList<RegulationSyncSchedulerHistoryDto>>
{
    public async Task<PaginatedList<RegulationSyncSchedulerHistoryDto>> Handle(GetRegulationSyncSchedulerHistoriesQuery request, CancellationToken ct)
    {
        var query = uow.RegulationSyncSchedulerHistories.Query()
            .AsNoTracking()
            .Include(x => x.GovernmentEntity)
            .AsQueryable();

        query = (request.SortBy?.ToLowerInvariant(), request.SortDir?.ToLowerInvariant()) switch
        {
            ("startedat", "asc") => query.OrderBy(x => x.StartedAt),
            ("startedat", _) => query.OrderByDescending(x => x.StartedAt),
            ("completedat", "asc") => query.OrderBy(x => x.CompletedAt),
            _ => query.OrderByDescending(x => x.CompletedAt),
        };

        var total = await query.CountAsync(ct);
        var projectedQuery = query.Select(x => new RegulationSyncSchedulerHistoryDto(
                x.Id,
                x.GovernmentEntityId,
                x.GovernmentEntity != null ? x.GovernmentEntity.TitleName : string.Empty,
                x.GovernmentEntity != null ? x.GovernmentEntity.TitleNumber : 0,
                x.OperationType,
                x.Status,
                x.TriggerSource,
                x.ImportedRecordsCount,
                x.ChangedRecordsCount,
                x.DeactivatedRecordsCount,
                x.OutboxEventsQueuedCount,
                x.Details,
                x.StartedAt,
                x.CompletedAt));

        if (request.PageSize > 0)
        {
            projectedQuery = projectedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);
        }

        var items = await projectedQuery.ToListAsync(ct);

        return new PaginatedList<RegulationSyncSchedulerHistoryDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
