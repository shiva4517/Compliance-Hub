using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetAvailableSubscriptionsForEmployee;

public record GetAvailableSubscriptionsForEmployeeQuery(Guid EmployeeId, Guid CustomerId)
    : IRequest<List<AvailableSubscriptionDto>>;

public record AvailableSubscriptionDto(
    Guid Id,
    SubscribingLevel SubscribingLevel,
    string SubscribedNodeName,
    Guid GovernmentEntityId,
    string GovernmentEntityName);

public class GetAvailableSubscriptionsForEmployeeQueryHandler(IUnitOfWork uow, IRegulationsUnitOfWork regulationsUow)
    : IRequestHandler<GetAvailableSubscriptionsForEmployeeQuery, List<AvailableSubscriptionDto>>
{
    public async Task<List<AvailableSubscriptionDto>> Handle(
        GetAvailableSubscriptionsForEmployeeQuery request, CancellationToken ct)
    {
        var alreadyAssignedSubIds = await uow.EmployeeAssignedWorks.Query()
            .Where(a => a.EmployeeId == request.EmployeeId && a.IsActive)
            .Select(a => a.SubscriptionId)
            .ToListAsync(ct);

        var subscriptions = await uow.Subscriptions.Query()
            .Where(s => s.CustomerId == request.CustomerId
                     && s.IsActive
                     && !alreadyAssignedSubIds.Contains(s.Id))
            .ToListAsync(ct);

        if (subscriptions.Count == 0) return [];

        var govEntityIds = subscriptions.Select(s => s.GovernmentEntityId).Distinct().ToList();

        var govEntities = await regulationsUow.GovernmentEntities.Query()
            .Where(e => govEntityIds.Contains(e.Id))
            .Select(e => new { e.Id, Name = e.TitleName })
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        return subscriptions.Select(s => new AvailableSubscriptionDto(
            s.Id,
            s.SubscribingLevel,
            s.SubscribedNodeName,
            s.GovernmentEntityId,
            govEntities.GetValueOrDefault(s.GovernmentEntityId, "—")
        )).ToList();
    }
}
