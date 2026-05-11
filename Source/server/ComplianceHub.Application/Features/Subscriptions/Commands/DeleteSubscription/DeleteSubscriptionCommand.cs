using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Subscriptions.Commands.DeleteSubscription;

public record DeleteSubscriptionCommand(Guid Id) : IRequest;

public class DeleteSubscriptionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeleteSubscriptionCommand>
{
    public async Task Handle(DeleteSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await uow.Subscriptions.Query()
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Subscription {request.Id} not found.");

        subscription.IsDeleted = true;
        subscription.IsActive = false;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.UpdatedBy = currentUser.Email;

        uow.Subscriptions.Update(subscription);
        await uow.SaveChangesAsync(ct);
    }
}
