using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.RegulationDetails.Commands.UpsertRegulationDetail;

public record UpsertRegulationDetailCommand(
    Guid RegulationId,
    string? Description,
    string? Condition,
    string? SuggestedTask,
    Guid? FrequencyTypeId,
    Guid? DueDateTypeId) : IRequest<Guid>;

public class UpsertRegulationDetailCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpsertRegulationDetailCommand, Guid>
{
    public async Task<Guid> Handle(UpsertRegulationDetailCommand request, CancellationToken ct)
    {
        var existing = await uow.RegulationDetails.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.RegulationId == request.RegulationId, ct);

        if (existing is not null)
        {
            existing.Description = request.Description;
            existing.Condition = request.Condition;
            existing.SuggestedTask = request.SuggestedTask;
            existing.FrequencyTypeId = request.FrequencyTypeId;
            existing.DueDateTypeId = request.DueDateTypeId;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = currentUser.Email;
            existing.IsDeleted = false;

            uow.RegulationDetails.Update(existing);
            await uow.SaveChangesAsync(ct);
            return existing.Id;
        }

        var detail = new RegulationDetail
        {
            RegulationId = request.RegulationId,
            Description = request.Description,
            Condition = request.Condition,
            SuggestedTask = request.SuggestedTask,
            FrequencyTypeId = request.FrequencyTypeId,
            DueDateTypeId = request.DueDateTypeId,
            CreatedBy = currentUser.Email
        };

        await uow.RegulationDetails.AddAsync(detail, ct);
        await uow.SaveChangesAsync(ct);
        return detail.Id;
    }
}
