using System.Text.Json;
using ComplianceHub.Functions.Data;
using ComplianceHub.Functions.Services;
using ComplianceHub.Functions.Telemetry;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Functions.Functions;

public class NotificationSenderFunction(
    IServiceScopeFactory scopeFactory,
    AppInsightsTelemetry telemetry,
    ILogger<NotificationSenderFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [Function("NotificationSender")]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken ct)
    {
        var messageId = message.MessageId;
        logger.LogInformation(
            "[NotificationSender] Message received. MessageId: {MessageId}, EnqueuedAt: {EnqueuedAt}",
            messageId, message.EnqueuedTime);

        telemetry.TrackEvent("SBMessageReceived", new Dictionary<string, string>
        {
            ["MessageId"] = messageId
        });

        Guid notificationHistoryId;
        try
        {
            var body = message.Body.ToString();
            logger.LogDebug("[NotificationSender] Payload: {Body}", body);

            var payload = JsonSerializer.Deserialize<SbPayload>(body, JsonOpts)
                ?? throw new InvalidOperationException("Deserialised payload was null.");

            notificationHistoryId = payload.NotificationHistoryId;

            logger.LogDebug(
                "[NotificationSender] Parsed NotificationHistoryId: {Id}", notificationHistoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[NotificationSender] Invalid message payload. MessageId: {MessageId}. Dead-lettering.",
                messageId);

            telemetry.TrackException(ex, new Dictionary<string, string>
            {
                ["MessageId"] = messageId,
                ["Stage"] = "PayloadParse"
            });

            await messageActions.DeadLetterMessageAsync(
                message, null, "InvalidPayload", ex.Message, ct);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var notification = await db.NotificationHistory.FindAsync(
            new object[] { notificationHistoryId }, ct);

        if (notification is null)
        {
            logger.LogWarning(
                "[NotificationSender] NotificationHistory {Id} not found in DB. Completing message.",
                notificationHistoryId);

            telemetry.TrackEvent("NotificationNotFound", new Dictionary<string, string>
            {
                ["NotificationHistoryId"] = notificationHistoryId.ToString(),
                ["MessageId"] = messageId
            });

            await messageActions.CompleteMessageAsync(message, ct);
            return;
        }

        if (notification.IsNotified)
        {
            logger.LogInformation(
                "[NotificationSender] Notification {Id} already sent (IsNotified=true). " +
                "Duplicate message — completing without re-sending. MessageId: {MessageId}",
                notificationHistoryId, messageId);

            telemetry.TrackEvent("DuplicateSkipped", new Dictionary<string, string>
            {
                ["NotificationHistoryId"] = notificationHistoryId.ToString(),
                ["MessageId"] = messageId
            });

            await messageActions.CompleteMessageAsync(message, ct);
            return;
        }

        logger.LogInformation(
            "[NotificationSender] Processing Notification {Id}. Recipient: {Recipient}.",
            notificationHistoryId, notification.RecipientEmail);

        var emailSender = scope.ServiceProvider.GetRequiredService<EmailSenderService>();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var success = await emailSender.SendWithRetryAsync(notification, ct);
        sw.Stop();

        if (success)
        {
            logger.LogInformation(
                "[NotificationSender] Email delivered for Notification {Id} in {Ms}ms. " +
                "Completing SB message {MessageId}.",
                notificationHistoryId, sw.ElapsedMilliseconds, messageId);

            telemetry.TrackDependency("SMTP", "Email", "SendEmail", true, sw.Elapsed);
            telemetry.TrackEvent("NotificationDelivered", new Dictionary<string, string>
            {
                ["NotificationHistoryId"] = notificationHistoryId.ToString(),
                ["MessageId"] = messageId,
                ["DurationMs"] = sw.ElapsedMilliseconds.ToString()
            });

            await messageActions.CompleteMessageAsync(message, ct);
        }
        else
        {
            logger.LogError(
                "[NotificationSender] All retry attempts exhausted for Notification {Id}. " +
                "Dead-lettering SB message {MessageId}.",
                notificationHistoryId, messageId);

            telemetry.TrackDependency("SMTP", "Email", "SendEmail", false, sw.Elapsed);
            telemetry.TrackEvent("NotificationDeadLettered", new Dictionary<string, string>
            {
                ["NotificationHistoryId"] = notificationHistoryId.ToString(),
                ["MessageId"] = messageId
            });

            await messageActions.DeadLetterMessageAsync(
                message,
                null,
                "MaxRetriesExceeded",
                $"All email send attempts failed for Notification {notificationHistoryId}.",
                ct);
        }
    }

    private sealed record SbPayload(Guid NotificationHistoryId);
}
