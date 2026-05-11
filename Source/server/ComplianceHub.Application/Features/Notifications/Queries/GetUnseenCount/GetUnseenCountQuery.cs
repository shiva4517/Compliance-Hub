using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Notifications.Queries.GetUnseenCount;

public record GetUnseenCountQuery(Guid CompanyId) : IRequest<int>;

public class GetUnseenCountQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetUnseenCountQuery, int>
{
    public Task<int> Handle(GetUnseenCountQuery request, CancellationToken ct)
        => uow.NotificationHistory.Query()
            .CountAsync(n => n.CompanyId == request.CompanyId && !n.IsSeen, ct);
}
