using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Subscriptions.Queries.GetSubscriptionDetails;

public record GetSubscriptionDetailsQuery(Guid SubscriptionId) : IRequest<SubscriptionDetailsDto?>;

public record SubscriptionDetailsDto(
    Guid SubscriptionId,
    Guid CustomerId,
    string CustomerName,
    SubscribingLevel Level,
    string NodeName,
    string Breadcrumb,
    IReadOnlyList<AgencyHierarchyDto> Agencies);

public class GetSubscriptionDetailsQueryHandler(IUnitOfWork uow, IRegulationsUnitOfWork regUow)
    : IRequestHandler<GetSubscriptionDetailsQuery, SubscriptionDetailsDto?>
{
    public async Task<SubscriptionDetailsDto?> Handle(GetSubscriptionDetailsQuery request, CancellationToken ct)
    {
        var sub = await uow.Subscriptions.Query()
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, ct);

        if (sub is null) return null;

        var breadcrumb = await BuildBreadcrumbAsync(sub, ct);
        var agencies = await BuildSubtreeAsync(sub, ct);

        return new SubscriptionDetailsDto(
            sub.Id, sub.CustomerId, sub.Customer!.CustomerName,
            sub.SubscribingLevel, sub.SubscribedNodeName, breadcrumb, agencies);
    }

    private async Task<string> BuildBreadcrumbAsync(Domain.Entities.Subscription sub, CancellationToken ct)
    {
        var entity = await regUow.GovernmentEntities.Query()
            .FirstOrDefaultAsync(e => e.Id == sub.GovernmentEntityId, ct);
        var parts = new List<string> { $"Title {entity?.TitleNumber} — {entity?.TitleName}" };

        if (sub.AgencyId.HasValue)
        {
            var agency = await regUow.Agencies.Query().FirstOrDefaultAsync(a => a.Id == sub.AgencyId, ct);
            if (agency is not null) parts.Add(agency.AgencyName);
        }
        if (sub.RegulationCategoryId.HasValue)
        {
            var cat = await regUow.RegulationCategories.Query().FirstOrDefaultAsync(c => c.Id == sub.RegulationCategoryId, ct);
            if (cat is not null) parts.Add(cat.SubchapterName);
        }
        if (sub.RegulationTypeId.HasValue)
        {
            var type = await regUow.RegulationTypes.Query().FirstOrDefaultAsync(t => t.Id == sub.RegulationTypeId, ct);
            if (type is not null) parts.Add(type.PartName);
        }
        if (sub.RegulationSubtypeId.HasValue)
        {
            var subtype = await regUow.RegulationSubtypes.Query().FirstOrDefaultAsync(s => s.Id == sub.RegulationSubtypeId, ct);
            if (subtype is not null) parts.Add(subtype.SubpartName);
        }
        if (sub.RegulationId.HasValue)
        {
            var reg = await regUow.Regulations.Query().FirstOrDefaultAsync(r => r.Id == sub.RegulationId, ct);
            if (reg is not null) parts.Add($"§ {reg.SectionNumber} {reg.SectionName}");
        }

        return string.Join(" > ", parts);
    }

    private async Task<IReadOnlyList<AgencyHierarchyDto>> BuildSubtreeAsync(Domain.Entities.Subscription sub, CancellationToken ct)
    {
        // Determine scope based on subscribing level and filter accordingly
        var agencyFilter = sub.AgencyId.HasValue
            ? sub.AgencyId.Value
            : (Guid?)null;

        var agenciesQuery = regUow.Agencies.Query()
            .IgnoreQueryFilters()
            .Where(a => a.GovernmentEntityId == sub.GovernmentEntityId);

        if (agencyFilter.HasValue)
            agenciesQuery = agenciesQuery.Where(a => a.Id == agencyFilter.Value);

        var agencies = await agenciesQuery.OrderBy(a => a.ChapterNumber).ToListAsync(ct);
        var agencyDtos = new List<AgencyHierarchyDto>();

        foreach (var agency in agencies)
        {
            var categoriesQuery = regUow.RegulationCategories.Query()
                .IgnoreQueryFilters()
                .Where(c => c.AgencyId == agency.Id);

            if (sub.RegulationCategoryId.HasValue)
                categoriesQuery = categoriesQuery.Where(c => c.Id == sub.RegulationCategoryId.Value);

            var categories = await categoriesQuery.OrderBy(c => c.SubchapterIdentifier).ToListAsync(ct);
            var categoryDtos = new List<CategoryHierarchyDto>();

            foreach (var category in categories)
            {
                var typesQuery = regUow.RegulationTypes.Query()
                    .IgnoreQueryFilters()
                    .Where(t => t.RegulationCategoryId == category.Id);

                if (sub.RegulationTypeId.HasValue)
                    typesQuery = typesQuery.Where(t => t.Id == sub.RegulationTypeId.Value);

                var types = await typesQuery.OrderBy(t => t.PartNumber).ToListAsync(ct);
                var typeDtos = new List<TypeHierarchyDto>();

                foreach (var type in types)
                {
                    var subtypesQuery = regUow.RegulationSubtypes.Query()
                        .IgnoreQueryFilters()
                        .Where(s => s.RegulationTypeId == type.Id);

                    if (sub.RegulationSubtypeId.HasValue)
                        subtypesQuery = subtypesQuery.Where(s => s.Id == sub.RegulationSubtypeId.Value);

                    var subtypes = await subtypesQuery.OrderBy(s => s.SubpartIdentifier).ToListAsync(ct);
                    var subtypeDtos = new List<SubtypeHierarchyDto>();

                    foreach (var subtype in subtypes)
                    {
                        var sectionsQuery = regUow.Regulations.Query()
                            .IgnoreQueryFilters()
                            .Where(r => r.RegulationTypeId == type.Id && r.RegulationSubtypeId == subtype.Id);

                        if (sub.RegulationId.HasValue)
                            sectionsQuery = sectionsQuery.Where(r => r.Id == sub.RegulationId.Value);

                        var sections = await sectionsQuery
                            .OrderBy(r => r.SectionNumber)
                            .Select(r => new SectionDto(r.Id, r.SectionNumber, r.SectionName, r.Version, r.IsActive, r.LastAmendedDate, null))
                            .ToListAsync(ct);

                        subtypeDtos.Add(new SubtypeHierarchyDto(subtype.Id, subtype.SubpartIdentifier, subtype.SubpartName, sections));
                    }

                    // Sections without subtype
                    var sectionsNoSubtype = regUow.Regulations.Query()
                        .IgnoreQueryFilters()
                        .Where(r => r.RegulationTypeId == type.Id && r.RegulationSubtypeId == null);

                    if (sub.RegulationId.HasValue)
                        sectionsNoSubtype = sectionsNoSubtype.Where(r => r.Id == sub.RegulationId.Value);

                    var noSubtypeSections = await sectionsNoSubtype
                        .OrderBy(r => r.SectionNumber)
                        .Select(r => new SectionDto(r.Id, r.SectionNumber, r.SectionName, r.Version, r.IsActive, r.LastAmendedDate, null))
                        .ToListAsync(ct);

                    if (noSubtypeSections.Count > 0)
                        subtypeDtos.Add(new SubtypeHierarchyDto(Guid.Empty, "NO_SUBPART", "No Subpart", noSubtypeSections));

                    typeDtos.Add(new TypeHierarchyDto(type.Id, type.PartNumber, type.PartName, subtypeDtos, []));
                }
                categoryDtos.Add(new CategoryHierarchyDto(category.Id, category.SubchapterIdentifier, category.SubchapterName, typeDtos));
            }
            agencyDtos.Add(new AgencyHierarchyDto(agency.Id, agency.ChapterNumber, agency.AgencyName, categoryDtos));
        }

        return agencyDtos;
    }
}
