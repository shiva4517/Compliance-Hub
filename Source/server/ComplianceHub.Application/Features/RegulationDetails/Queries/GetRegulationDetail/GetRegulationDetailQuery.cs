using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.RegulationDetails.Queries.GetRegulationDetail;

public record GetRegulationDetailQuery(Guid RegulationId) : IRequest<RegulationDetailDto?>;

public record RegulationDetailDto(
    Guid? Id,
    Guid RegulationId,
    string? Description,
    string? Condition,
    string? SuggestedTask,
    Guid? FrequencyTypeId,
    string? FrequencyTypeName,
    Guid? DueDateTypeId,
    string? DueDateTypeName);

public class GetRegulationDetailQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetRegulationDetailQuery, RegulationDetailDto?>
{
    public async Task<RegulationDetailDto?> Handle(GetRegulationDetailQuery request, CancellationToken ct)
    {
        var detail = await uow.RegulationDetails.Query()
            .Include(d => d.FrequencyType)
            .Include(d => d.DueDateType)
            .FirstOrDefaultAsync(d => d.RegulationId == request.RegulationId, ct);

        if (detail is null) return null;

        return new RegulationDetailDto(
            detail.Id,
            detail.RegulationId,
            detail.Description,
            detail.Condition,
            detail.SuggestedTask,
            detail.FrequencyTypeId,
            detail.FrequencyType?.Name,
            detail.DueDateTypeId,
            detail.DueDateType?.Name);
    }
}
