using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Subscriptions.Commands.UpsertSubscriptionDetail;

public record UpsertSubscriptionDetailCommand(
    Guid SubscriptionId,
    string? Description,
    string? Condition,
    string? SuggestedTask,
    Guid? FrequencyTypeId,
    Guid? DueDateTypeId,
    decimal? MinValue,
    decimal? MaxValue) : IRequest<Guid>;

public class UpsertSubscriptionDetailCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpsertSubscriptionDetailCommand, Guid>
{
    public async Task<Guid> Handle(UpsertSubscriptionDetailCommand request, CancellationToken ct)
    {
        var subscription = await uow.Subscriptions.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, ct)
            ?? throw new KeyNotFoundException("Subscription not found.");

        var existing = await uow.RegulationDetails.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.SubscriptionId == request.SubscriptionId, ct);

        if (existing is not null)
        {
            existing.Description = request.Description;
            existing.Condition = request.Condition;
            existing.SuggestedTask = request.SuggestedTask;
            existing.FrequencyTypeId = request.FrequencyTypeId;
            existing.DueDateTypeId = request.DueDateTypeId;
            existing.MinValue = request.MinValue;
            existing.MaxValue = request.MaxValue;
            existing.SubscribingLevel = subscription.SubscribingLevel;
            // RegulationId stays as the originating regulation when present;
            // for non-Regulation subscriptions we still carry the linked regulation id
            // if available so legacy joins keep working — otherwise leave Empty.
            existing.RegulationId = subscription.RegulationId ?? existing.RegulationId;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = currentUser.Email;
            existing.IsDeleted = false;

            uow.RegulationDetails.Update(existing);
            await uow.SaveChangesAsync(ct);
            return existing.Id;
        }

        var detail = new RegulationDetail
        {
            SubscriptionId = request.SubscriptionId,
            SubscribingLevel = subscription.SubscribingLevel,
            RegulationId = subscription.RegulationId ?? Guid.Empty,
            Description = request.Description,
            Condition = request.Condition,
            SuggestedTask = request.SuggestedTask,
            FrequencyTypeId = request.FrequencyTypeId,
            DueDateTypeId = request.DueDateTypeId,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            CreatedBy = currentUser.Email
        };

        await uow.RegulationDetails.AddAsync(detail, ct);
        await uow.SaveChangesAsync(ct);
        return detail.Id;
    }
}
