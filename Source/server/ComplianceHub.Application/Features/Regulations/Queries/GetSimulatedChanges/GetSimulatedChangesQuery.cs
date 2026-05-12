using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetSimulatedChanges;

public record GetSimulatedChangesQuery(Guid GovernmentEntityId, DateOnly IssueDate)
    : IRequest<SimulatedChangesResultDto>;

public record SimulatedChangesResultDto(
    int TitleNumber,
    int TotalChanged,
    int MatchedInLocal,
    IReadOnlyList<SimulatedChangeItemDto> Items);

public record SimulatedChangeItemDto(
    Guid RegulationId,
    string SectionNumber,
    string SectionName,
    DateOnly? AmendmentDate,
    DateOnly? IssueDate,
    int CurrentVersion,
    string CurrentContentHash,
    DateOnly? CurrentLastAmendedDate,
    string CurrentHtmlContent);

public class GetSimulatedChangesQueryHandler(
    IRegulationsUnitOfWork uow,
    IECFRApiClient ecfr)
    : IRequestHandler<GetSimulatedChangesQuery, SimulatedChangesResultDto>
{
    public async Task<SimulatedChangesResultDto> Handle(GetSimulatedChangesQuery request, CancellationToken ct)
    {
        var entity = await uow.GovernmentEntities.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.GovernmentEntityId, ct)
            ?? throw new InvalidOperationException("Government entity not found.");

        var versions = await ecfr.GetVersionsSinceAsync(entity.TitleNumber, request.IssueDate, ct);
        var changes = versions?.ContentVersions ?? [];

        var identifiers = changes
            .Where(c => !string.IsNullOrWhiteSpace(c.Identifier))
            .Select(c => c.Identifier)
            .Distinct()
            .ToList();

        if (identifiers.Count == 0)
        {
            return new SimulatedChangesResultDto(entity.TitleNumber, changes.Count, 0, []);
        }

        var localRegs = await uow.Regulations.Query()
            .AsNoTracking()
            .Where(r => r.GovernmentEntityId == request.GovernmentEntityId
                        && r.IsActive
                        && identifiers.Contains(r.SectionNumber))
            .Select(r => new
            {
                r.Id,
                r.SectionNumber,
                r.SectionName,
                r.Version,
                r.ContentHash,
                r.LastAmendedDate,
                r.HtmlContent,
            })
            .ToListAsync(ct);

        var localBySection = localRegs.ToDictionary(r => r.SectionNumber, StringComparer.OrdinalIgnoreCase);

        var items = new List<SimulatedChangeItemDto>();
        foreach (var change in changes)
        {
            if (!localBySection.TryGetValue(change.Identifier, out var reg)) continue;

            items.Add(new SimulatedChangeItemDto(
                reg.Id,
                reg.SectionNumber,
                reg.SectionName,
                ParseDate(change.AmendmentDate),
                ParseDate(change.IssueDate),
                reg.Version,
                reg.ContentHash,
                reg.LastAmendedDate,
                reg.HtmlContent));
        }

        return new SimulatedChangesResultDto(entity.TitleNumber, changes.Count, items.Count, items);
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParse(value, out var d) ? d : null;
    }
}
