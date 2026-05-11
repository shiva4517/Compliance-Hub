using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Notifications.Queries.GetNotificationById;

public record GetNotificationByIdQuery(Guid Id, Guid CompanyId) : IRequest<NotificationLogDetailDto?>;

public record NotificationSentHistoryDto(
    Guid Id,
    int AttemptNumber,
    bool IsSuccess,
    string Status,
    string? FailureReason,
    DateTime AttemptedAt);

public record NotificationLogDetailDto(
    Guid Id,
    Guid SubscriptionId,
    Guid CustomerId,
    string? CustomerName,
    Guid? CompanyId,
    Guid? GovernmentEntityId,
    string? GovernmentEntityName,
    Guid? AgencyId,
    string? AgencyName,
    Guid? RegulationCategoryId,
    string? RegulationCategoryName,
    Guid? RegulationTypeId,
    string? RegulationTypeName,
    Guid? RegulationSubtypeId,
    string? RegulationSubtypeName,
    Guid? PreviousRegulationId,
    string? PreviousRegulationName,
    Guid? PresentRegulationId,
    string? PresentRegulationName,
    string? Subject,
    string? Body,
    string? SenderEmail,
    string? SenderName,
    string? RecipientEmail,
    string? RecipientName,
    bool IsNotified,
    string Status,
    int RetryCount,
    DateTime? LastRetryAt,
    string? FailureReason,
    DateTime CreatedAt,
    List<NotificationSentHistoryDto> SentHistory);

public class GetNotificationByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetNotificationByIdQuery, NotificationLogDetailDto?>
{
    public async Task<NotificationLogDetailDto?> Handle(GetNotificationByIdQuery request, CancellationToken ct)
    {
        var n = await uow.NotificationHistory.Query()
            .Include(n => n.Customer)
            .FirstOrDefaultAsync(n => n.Id == request.Id, ct);

        if (n is null) return null;

        if (n.CompanyId != request.CompanyId)
            throw new UnauthorizedAccessException("Access to this notification is not allowed.");

        var sentHistory = await uow.NotificationSentHistories.Query()
            .Where(h => h.NotificationHistoryId == request.Id)
            .OrderBy(h => h.AttemptNumber)
            .Select(h => new NotificationSentHistoryDto(
                h.Id, h.AttemptNumber, h.IsSuccess, h.Status, h.FailureReason, h.AttemptedAt))
            .ToListAsync(ct);

        return new NotificationLogDetailDto(
            n.Id, n.SubscriptionId, n.CustomerId,
            n.Customer?.CustomerName,
            n.CompanyId,
            n.GovernmentEntityId, n.GovernmentEntityName,
            n.AgencyId, n.AgencyName,
            n.RegulationCategoryId, n.RegulationCategoryName,
            n.RegulationTypeId, n.RegulationTypeName,
            n.RegulationSubtypeId, n.RegulationSubtypeName,
            n.PreviousRegulationId, n.PreviousRegulationName,
            n.PresentRegulationId, n.PresentRegulationName,
            n.Subject, n.Body,
            n.SenderEmail, n.SenderName,
            n.RecipientEmail, n.RecipientName,
            n.IsNotified, n.Status, n.RetryCount, n.LastRetryAt,
            n.FailureReason,
            n.CreatedAt,
            sentHistory);
    }
}
