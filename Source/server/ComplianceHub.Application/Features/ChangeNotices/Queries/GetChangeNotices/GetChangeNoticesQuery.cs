using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.ChangeNotices.Queries.GetChangeNotices;

public record GetChangeNoticesQuery(
    string? Role,
    Guid? ReferenceId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? Search,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<ChangeNoticeDto>>;

public record ChangeNoticeDto(
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

public class GetChangeNoticesQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetChangeNoticesQuery, PaginatedList<ChangeNoticeDto>>
{
    public async Task<PaginatedList<ChangeNoticeDto>> Handle(GetChangeNoticesQuery request, CancellationToken ct)
    {
        // Step 1: Resolve effective subscriptions for Employee / Customer
        List<Subscription>? effectiveSubs = null;

        if (request.Role == "Employee" && request.ReferenceId.HasValue)
        {
            var subIds = await uow.EmployeeAssignedWorks.Query()
                .Where(w => w.EmployeeId == request.ReferenceId.Value && w.IsActive && !w.IsDeleted)
                .Select(w => w.SubscriptionId)
                .ToListAsync(ct);

            var subs = await uow.Subscriptions.Query()
                .Where(s => subIds.Contains(s.Id) && s.IsActive && !s.IsDeleted)
                .ToListAsync(ct);

            effectiveSubs = Deduplicate(subs);
        }
        else if (request.Role == "Customer" && request.ReferenceId.HasValue)
        {
            var subs = await uow.Subscriptions.Query()
                .Where(s => s.CustomerId == request.ReferenceId.Value && s.IsActive && !s.IsDeleted)
                .ToListAsync(ct);

            effectiveSubs = Deduplicate(subs);
        }

        // Step 2: Apply date and search filters in SQL
        var query = uow.ChangeNotices.Query().AsQueryable();

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

        // Step 3: If subscription filtering needed — materialise first, then filter + page in memory
        if (effectiveSubs != null)
        {
            if (effectiveSubs.Count == 0)
                return new PaginatedList<ChangeNoticeDto>
                {
                    Items = [],
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

            var all = await query.OrderByDescending(c => c.ChangedAt).ToListAsync(ct);
            var matched = all.Where(c => MatchesAny(c, effectiveSubs)).ToList();

            return new PaginatedList<ChangeNoticeDto>
            {
                Items = matched
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(ToDto)
                    .ToList(),
                TotalCount = matched.Count,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        // Step 4: Admin — paginate in SQL
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.ChangedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PaginatedList<ChangeNoticeDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ChangeNoticeDto ToDto(ChangeNotice c) => new(
        c.Id, c.RegulationId, c.GovernmentEntityId, c.GovernmentEntityName,
        c.AgencyName, c.RegulationCategoryName, c.RegulationTypeName,
        c.RegulationSubtypeName, c.SectionNumber, c.SectionTitle,
        c.ArchivedVersion, c.NewVersion, c.ChangedAt);

    // Remove subscriptions already covered by a broader ancestor at the same level
    private static List<Subscription> Deduplicate(List<Subscription> subs)
    {
        var effective = new List<Subscription>();
        foreach (var sub in subs.OrderBy(s => (int)s.SubscribingLevel))
        {
            if (!effective.Any(e => Covers(e, sub)))
                effective.Add(sub);
        }
        return effective;
    }

    // Returns true when `ancestor` is a broader subscription that already covers `target`
    private static bool Covers(Subscription ancestor, Subscription target)
    {
        if ((int)ancestor.SubscribingLevel >= (int)target.SubscribingLevel) return false;
        if (ancestor.GovernmentEntityId != target.GovernmentEntityId) return false;
        if (ancestor.AgencyId.HasValue && ancestor.AgencyId != target.AgencyId) return false;
        if (ancestor.RegulationCategoryId.HasValue && ancestor.RegulationCategoryId != target.RegulationCategoryId) return false;
        if (ancestor.RegulationTypeId.HasValue && ancestor.RegulationTypeId != target.RegulationTypeId) return false;
        if (ancestor.RegulationSubtypeId.HasValue && ancestor.RegulationSubtypeId != target.RegulationSubtypeId) return false;
        return true;
    }

    private static bool MatchesAny(ChangeNotice c, List<Subscription> effective) =>
        effective.Any(s => s.SubscribingLevel switch
        {
            SubscribingLevel.Entity =>
                c.GovernmentEntityId == s.GovernmentEntityId,

            SubscribingLevel.Agency =>
                c.GovernmentEntityId == s.GovernmentEntityId &&
                c.AgencyId == s.AgencyId,

            SubscribingLevel.Category =>
                c.GovernmentEntityId == s.GovernmentEntityId &&
                c.AgencyId == s.AgencyId &&
                c.RegulationCategoryId == s.RegulationCategoryId,

            SubscribingLevel.Type =>
                c.GovernmentEntityId == s.GovernmentEntityId &&
                c.AgencyId == s.AgencyId &&
                c.RegulationCategoryId == s.RegulationCategoryId &&
                c.RegulationTypeId == s.RegulationTypeId,

            SubscribingLevel.SubType =>
                c.GovernmentEntityId == s.GovernmentEntityId &&
                c.AgencyId == s.AgencyId &&
                c.RegulationCategoryId == s.RegulationCategoryId &&
                c.RegulationTypeId == s.RegulationTypeId &&
                c.RegulationSubtypeId == s.RegulationSubtypeId,

            SubscribingLevel.Regulation =>
                c.RegulationId == s.RegulationId,

            _ => false
        });
}
