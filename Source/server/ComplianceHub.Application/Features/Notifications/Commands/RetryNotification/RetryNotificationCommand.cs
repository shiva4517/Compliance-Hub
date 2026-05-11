using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Notifications.Commands.RetryNotification;

public record RetryNotificationCommand(Guid Id, Guid CompanyId) : IRequest<RetryNotificationResult>;

public record RetryNotificationResult(bool Success, string Message);

public class RetryNotificationCommandHandler(IUnitOfWork uow, IEmailService emailService)
    : IRequestHandler<RetryNotificationCommand, RetryNotificationResult>
{
    private const int MaxAttempts = 3;
    private const int InitialDelaySeconds = 2;

    public async Task<RetryNotificationResult> Handle(RetryNotificationCommand request, CancellationToken ct)
    {
        var notification = await uow.NotificationHistory.Query()
            .FirstOrDefaultAsync(n => n.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Notification {request.Id} not found.");

        if (notification.CompanyId != request.CompanyId)
            throw new UnauthorizedAccessException("Access to this notification is not allowed.");

        if (notification.Status == "Sent" || notification.IsNotified)
            throw new InvalidOperationException("Notification has already been sent successfully.");

        if (string.IsNullOrWhiteSpace(notification.RecipientEmail))
            throw new InvalidOperationException("Notification is missing recipient email required for retry.");

        if (string.IsNullOrWhiteSpace(notification.Subject) || string.IsNullOrWhiteSpace(notification.Body))
        {
            var customerName = notification.CustomerId != Guid.Empty
                ? await uow.Customers.Query()
                    .Where(c => c.Id == notification.CustomerId)
                    .Select(c => c.CustomerName)
                    .FirstOrDefaultAsync(ct)
                : null;

            var regulationDetailsHtml = await BuildRegulationDetailsHtmlAsync(notification, ct);
            var changeType = ResolveChangeType(notification);
            var recipientName = FirstNonEmpty(notification.RecipientName, customerName, "Customer");
            var resolvedCustomerName = FirstNonEmpty(customerName, notification.Customer?.CustomerName, "Customer");
            var governmentEntityName = FirstNonEmpty(notification.GovernmentEntityName, "Federal Regulation");
            var previousRegulationName = FirstNonEmpty(notification.PreviousRegulationName, "Not available");
            var presentRegulationName = FirstNonEmpty(notification.PresentRegulationName, "Not available");

            var rebuilt = emailService.BuildRegulationChangeEmail(
                recipientName,
                resolvedCustomerName,
                governmentEntityName,
                previousRegulationName,
                presentRegulationName,
                changeType,
                regulationDetailsHtml);

            notification.Subject = rebuilt.Subject;
            notification.Body = rebuilt.Body;
            notification.LastRetryAt = DateTime.UtcNow;

            uow.NotificationHistory.Update(notification);
            await uow.SaveChangesAsync(ct);
        }

        var currentAttempt = await uow.NotificationSentHistories.Query()
            .Where(h => h.NotificationHistoryId == request.Id)
            .Select(h => (int?)h.AttemptNumber)
            .MaxAsync(ct) ?? 0;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await emailService.SendRawEmailOrThrowAsync(
                    notification.RecipientEmail,
                    notification.Subject,
                    notification.Body,
                    ct);

                await uow.NotificationSentHistories.AddAsync(new NotificationSentHistory
                {
                    NotificationHistoryId = notification.Id,
                    AttemptNumber = currentAttempt + attempt,
                    IsSuccess = true,
                    Status = "Sent",
                    AttemptedAt = DateTime.UtcNow
                }, ct);

                notification.Status = "Sent";
                notification.IsNotified = true;
                notification.RetryCount = 0;
                notification.LastRetryAt = DateTime.UtcNow;
                notification.FailureReason = null;

                uow.NotificationHistory.Update(notification);
                await uow.SaveChangesAsync(ct);

                return new RetryNotificationResult(true, "Notification email sent successfully.");
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                var isLastAttempt = attempt == MaxAttempts;

                await uow.NotificationSentHistories.AddAsync(new NotificationSentHistory
                {
                    NotificationHistoryId = notification.Id,
                    AttemptNumber = currentAttempt + attempt,
                    IsSuccess = false,
                    Status = isLastAttempt ? "DeadLettered" : "Failed",
                    FailureReason = ex.Message,
                    AttemptedAt = DateTime.UtcNow
                }, ct);

                if (isLastAttempt)
                {
                    notification.Status = "Failed";
                    notification.IsNotified = false;
                    notification.RetryCount += 1;
                    notification.LastRetryAt = DateTime.UtcNow;
                    notification.FailureReason = ex.Message;

                    uow.NotificationHistory.Update(notification);
                    await uow.SaveChangesAsync(ct);

                    return new RetryNotificationResult(false, $"Notification retry failed: {ex.Message}");
                }

                await uow.SaveChangesAsync(ct);

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) * InitialDelaySeconds);
                await Task.Delay(delay, ct);
            }
        }

        return new RetryNotificationResult(false, "Notification retry failed.");
    }

    private async Task<string?> BuildRegulationDetailsHtmlAsync(NotificationHistory notification, CancellationToken ct)
    {
        var regulationId = notification.PresentRegulationId ?? notification.PreviousRegulationId;
        if (!regulationId.HasValue)
        {
            return null;
        }

        var detail = await uow.RegulationDetails.Query()
            .Include(d => d.FrequencyType)
            .Include(d => d.DueDateType)
            .FirstOrDefaultAsync(d => d.RegulationId == regulationId.Value, ct);

        if (detail is null)
        {
            return null;
        }

        var sections = new List<string>();

        if (!string.IsNullOrWhiteSpace(detail.Description))
            sections.Add(BuildDetailBlock("Description", detail.Description));
        if (!string.IsNullOrWhiteSpace(detail.Condition))
            sections.Add(BuildDetailBlock("Condition", detail.Condition));
        if (!string.IsNullOrWhiteSpace(detail.SuggestedTask))
            sections.Add(BuildDetailBlock("Suggested Task", detail.SuggestedTask));
        if (!string.IsNullOrWhiteSpace(detail.FrequencyType?.Name))
            sections.Add(BuildDetailBlock("Frequency", detail.FrequencyType.Name));
        if (!string.IsNullOrWhiteSpace(detail.DueDateType?.Name))
            sections.Add(BuildDetailBlock("Due Date Type", detail.DueDateType.Name));

        return sections.Count == 0 ? null : string.Join(Environment.NewLine, sections);
    }

    private static string BuildDetailBlock(string label, string value)
    {
        var encodedLabel = System.Net.WebUtility.HtmlEncode(label);
        var encodedValue = System.Net.WebUtility.HtmlEncode(value);
        return $"<p><strong>{encodedLabel}:</strong> {encodedValue}</p>";
    }

    private static string ResolveChangeType(NotificationHistory notification)
    {
        var hasPrevious = !string.IsNullOrWhiteSpace(notification.PreviousRegulationName);
        var hasPresent = !string.IsNullOrWhiteSpace(notification.PresentRegulationName);

        return (hasPrevious, hasPresent) switch
        {
            (false, true) => "Create",
            (true, false) => "Remove",
            _ => "Update"
        };
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
}
