using ComplianceHub.Functions.Data;
using ComplianceHub.Functions.Data.Models;
using ComplianceHub.Functions.Options;
using ComplianceHub.Functions.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ComplianceHub.Functions.Services;

public class EmailSenderService(
    NotificationDbContext db,
    EmailTemplateService templateService,
    IOptions<EmailOptions> emailOpts,
    IOptions<RetryOptions> retryOpts,
    AppInsightsTelemetry telemetry,
    ILogger<EmailSenderService> logger)
{
    private readonly EmailOptions _email = emailOpts.Value;
    private readonly RetryOptions _retry = retryOpts.Value;

    /// <returns>true = email sent successfully; false = all attempts exhausted.</returns>
    public async Task<bool> SendWithRetryAsync(NotificationHistory notification, CancellationToken ct)
    {
        var recipientEmail = notification.RecipientEmail;
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            logger.LogWarning("Notification {Id} has no RecipientEmail — skipping.", notification.Id);
            await MarkFailedAsync(notification, "No recipient email address.", ct);
            return false;
        }

        var htmlBody = templateService.Resolve(notification);
        var subject = string.IsNullOrWhiteSpace(notification.Subject)
            ? $"Regulation Change Notice — {notification.GovernmentEntityName ?? "Compliance Hub"}"
            : notification.Subject;

        for (int attempt = 1; attempt <= _retry.MaxAttempts; attempt++)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await SendCoreAsync(notification, subject, htmlBody, ct);
                sw.Stop();

                logger.LogInformation(
                    "Email sent for Notification {Id} on attempt {Attempt}. Recipient: {To}",
                    notification.Id, attempt, recipientEmail);

                telemetry.TrackDependency("SendGrid", "sendgrid.com", "SendEmail", true, sw.Elapsed);
                telemetry.TrackEvent("EmailSent", new Dictionary<string, string>
                {
                    ["NotificationId"] = notification.Id.ToString(),
                    ["Attempt"]        = attempt.ToString(),
                    ["Recipient"]      = recipientEmail
                });

                await RecordAttemptAsync(notification.Id, attempt, true, "Sent", null, ct);
                await MarkSentAsync(notification, ct);
                return true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                telemetry.TrackDependency("SendGrid", "sendgrid.com", "SendEmail", false, sw.Elapsed);
                telemetry.TrackException(ex, new Dictionary<string, string>
                {
                    ["NotificationId"] = notification.Id.ToString(),
                    ["Attempt"]        = attempt.ToString()
                });

                bool isLast = attempt == _retry.MaxAttempts;
                var status  = isLast ? "DeadLettered" : "Failed";

                logger.LogWarning(ex,
                    "Email attempt {Attempt}/{Max} failed for Notification {Id}. Status: {Status}",
                    attempt, _retry.MaxAttempts, notification.Id, status);

                await RecordAttemptAsync(notification.Id, attempt, false, status, ex.Message, ct);

                if (isLast)
                {
                    await MarkFailedAsync(notification, ex.Message, ct);
                    return false;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) * _retry.InitialDelaySeconds);
                logger.LogDebug("Waiting {Delay}s before retry {Next}.", delay.TotalSeconds, attempt + 1);
                await Task.Delay(delay, ct);
            }
        }

        return false;
    }

    private async Task SendCoreAsync(
        NotificationHistory n, string subject, string htmlBody, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_email.SendGridApiKey))
            throw new InvalidOperationException("Email:SendGridApiKey is not configured.");

        var client  = new SendGridClient(_email.SendGridApiKey);
        var from    = new EmailAddress(_email.FromAddress, _email.FromName);
        var to      = new EmailAddress(n.RecipientEmail, n.RecipientName ?? n.RecipientEmail);
        var message = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlBody);

        var response = await client.SendEmailAsync(message, ct);
        var statusCode = (int)response.StatusCode;

        if (statusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            logger.LogError(
                "SendGrid rejected email for Notification {Id} — HTTP {StatusCode}. Body: {ResponseBody}",
                n.Id, statusCode, body);
            throw new InvalidOperationException($"SendGrid returned {statusCode}: {body}");
        }
    }

    private async Task RecordAttemptAsync(
        Guid notificationId, int attempt, bool success, string status, string? reason, CancellationToken ct)
    {
        db.NotificationSentHistory.Add(new NotificationSentHistory
        {
            NotificationHistoryId = notificationId,
            AttemptNumber         = attempt,
            IsSuccess             = success,
            Status                = status,
            FailureReason         = reason,
            AttemptedAt           = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task MarkSentAsync(NotificationHistory n, CancellationToken ct)
    {
        n.IsNotified  = true;
        n.Status      = "Sent";
        n.RetryCount  = 0;
        n.LastRetryAt = DateTime.UtcNow;
        db.NotificationHistory.Update(n);
        await db.SaveChangesAsync(ct);
    }

    private async Task MarkFailedAsync(NotificationHistory n, string reason, CancellationToken ct)
    {
        n.IsNotified     = false;
        n.Status         = "Failed";
        n.FailureReason  = reason;
        n.RetryCount++;
        n.LastRetryAt    = DateTime.UtcNow;
        db.NotificationHistory.Update(n);
        await db.SaveChangesAsync(ct);
    }
}
