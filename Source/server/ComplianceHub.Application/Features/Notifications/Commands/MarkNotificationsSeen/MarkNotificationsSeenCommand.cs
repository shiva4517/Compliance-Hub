using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Notifications.Commands.MarkNotificationsSeen;

public record MarkNotificationsSeenCommand(Guid CompanyId) : IRequest;

public class MarkNotificationsSeenCommandHandler(IUnitOfWork uow)
    : IRequestHandler<MarkNotificationsSeenCommand>
{
    public async Task Handle(MarkNotificationsSeenCommand request, CancellationToken ct)
    {
        var unseen = await uow.NotificationHistory.Query()
            .Where(n => n.CompanyId == request.CompanyId && !n.IsSeen)
            .ToListAsync(ct);

        if (unseen.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var n in unseen)
        {
            n.IsSeen = true;
            n.SeenAt = now;
            uow.NotificationHistory.Update(n);
        }

        await uow.SaveChangesAsync(ct);
    }
}
