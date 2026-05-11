using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;

public record GetRegulationHierarchyQuery(Guid GovernmentEntityId) : IRequest<RegulationHierarchyDto?>;

public record RegulationHierarchyDto(Guid GovernmentEntityId, int TitleNumber, string TitleName, IReadOnlyList<AgencyHierarchyDto> Agencies);
public record AgencyHierarchyDto(Guid Id, string ChapterNumber, string AgencyName, IReadOnlyList<CategoryHierarchyDto> Categories);
public record CategoryHierarchyDto(Guid Id, string SubchapterIdentifier, string SubchapterName, IReadOnlyList<TypeHierarchyDto> Types);
public record TypeHierarchyDto(Guid Id, int PartNumber, string PartName, IReadOnlyList<SubtypeHierarchyDto> Subtypes, IReadOnlyList<SectionDto> Sections);
public record SubtypeHierarchyDto(Guid Id, string SubpartIdentifier, string SubpartName, IReadOnlyList<SectionDto> Sections);
public record SectionDto(Guid Id, string SectionNumber, string SectionName, int Version, bool IsActive, DateOnly? LastAmendedDate, string? HtmlContent);

public class GetRegulationHierarchyQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetRegulationHierarchyQuery, RegulationHierarchyDto?>
{
    public async Task<RegulationHierarchyDto?> Handle(GetRegulationHierarchyQuery request, CancellationToken ct)
    {
        var entity = await uow.GovernmentEntities.Query()
            .FirstOrDefaultAsync(e => e.Id == request.GovernmentEntityId, ct);

        if (entity is null) return null;

        var hierarchy = await uow.GovernmentEntities.Query()
     .IgnoreQueryFilters()
     .Where(e => e.Id == request.GovernmentEntityId)
     .Select(entity => new RegulationHierarchyDto(
         entity.Id,
         entity.TitleNumber,
         entity.TitleName,
         entity.Agencies
             .OrderBy(a => a.ChapterNumber)
             .Select(agency => new AgencyHierarchyDto(
                 agency.Id,
                 agency.ChapterNumber,
                 agency.AgencyName,
                 agency.RegulationCategories
                     .OrderBy(c => c.SubchapterIdentifier)
                     .Select(category => new CategoryHierarchyDto(
                         category.Id,
                         category.SubchapterIdentifier,
                         category.SubchapterName,
                         category.RegulationTypes
                             .OrderBy(t => t.PartNumber)
                             .Select(type => new TypeHierarchyDto(
                                 type.Id,
                                 type.PartNumber,
                                 type.PartName,
                                 type.RegulationSubtypes
                                     .OrderBy(s => s.SubpartIdentifier)
                                     .Select(subtype => new SubtypeHierarchyDto(
                                         subtype.Id,
                                         subtype.SubpartIdentifier,
                                         subtype.SubpartName,
                                         subtype.Regulations
                                             .OrderBy(r => r.SectionNumber)
                                             .Select(r => new SectionDto(r.Id, r.SectionNumber, r.SectionName, r.Version, r.IsActive, r.LastAmendedDate, r.HtmlContent))
                                             .ToList()
                                     )).ToList(),
                                 // Direct regulations under the type (no subtype)
                                 type.Regulations
                                     .Where(r => r.RegulationSubtypeId == null)
                                     .OrderBy(r => r.SectionNumber)
                                     .Select(r => new SectionDto(r.Id, r.SectionNumber, r.SectionName, r.Version, r.IsActive, r.LastAmendedDate, r.HtmlContent))
                                     .ToList()
                             )).ToList()
                     )).ToList()
             )).ToList()
     ))
     .FirstOrDefaultAsync(ct);

        return hierarchy;
    }
}
