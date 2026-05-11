using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.RegulationChangeLogs.Queries.GetRegulationChangeLogById;

public record GetRegulationChangeLogByIdQuery(Guid Id) : IRequest<RegulationChangeLogDetailDto?>;

public record RegulationChangeLogDetailDto(
    Guid Id,
    Guid RegulationId,
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
    string? PreviousContentHash,
    string? NewContentHash,
    string? PreviousHtmlContent,
    string? NewHtmlContent,
    DateOnly? PreviousAmendedDate,
    DateOnly? NewAmendedDate,
    int ArchivedVersion,
    int NewVersion,
    DateTime ChangedAt,
    string? CreatedBy);

public class GetRegulationChangeLogByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetRegulationChangeLogByIdQuery, RegulationChangeLogDetailDto?>
{
    public async Task<RegulationChangeLogDetailDto?> Handle(GetRegulationChangeLogByIdQuery request, CancellationToken ct)
    {
        var r = await uow.ChangeNotices.Query()
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct);

        if (r is null) return null;

        return new RegulationChangeLogDetailDto(
            r.Id, r.RegulationId,
            r.GovernmentEntityId, r.GovernmentEntityName,
            r.AgencyId, r.AgencyName,
            r.RegulationCategoryId, r.RegulationCategoryName,
            r.RegulationTypeId, r.RegulationTypeName,
            r.RegulationSubtypeId, r.RegulationSubtypeName,
            r.SectionNumber, r.SectionTitle,
            r.PreviousContentHash, r.NewContentHash,
            r.PreviousHtmlContent, r.NewHtmlContent,
            r.PreviousAmendedDate, r.NewAmendedDate,
            r.ArchivedVersion, r.NewVersion,
            r.ChangedAt, r.CreatedBy);
    }
}
