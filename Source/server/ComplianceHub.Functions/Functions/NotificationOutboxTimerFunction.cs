using System.Text;
using System.Text.Json;
using ComplianceHub.Functions.Data;
using ComplianceHub.Functions.Options;
using ComplianceHub.Functions.Telemetry;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplianceHub.Functions.Functions;

public class NotificationOutboxTimerFunction(
    IServiceScopeFactory scopeFactory,
    ServiceBusSender sbSender,
    AppInsightsTelemetry telemetry,
    IOptions<OutboxOptions> outboxOpts,
    ILogger<NotificationOutboxTimerFunction> logger)
{
    private readonly OutboxOptions _outbox = outboxOpts.Value;

    [Function("NotificationOutboxTimer")]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer,
        CancellationToken ct)
    {
        logger.LogInformation(
            "[OutboxTimer] Triggered at {Time}. IsPastDue: {IsPastDue}",
            DateTime.UtcNow, timer.IsPastDue);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        await ResetStaleProcessingRowsAsync(db, ct);
        await PublishPendingBatchAsync(db, ct);

        logger.LogInformation("[OutboxTimer] Completed at {Time}.", DateTime.UtcNow);
    }

    private async Task ResetStaleProcessingRowsAsync(NotificationDbContext db, CancellationToken ct)
    {
        var staleThreshold = DateTime.UtcNow.AddMinutes(-_outbox.StaleProcessingThresholdMinutes);

        var staleRows = await db.NotificationOutbox
            .Where(o => o.Status != "Published" && o.CreatedAt < staleThreshold)
            .ToListAsync(ct);

        if (staleRows.Count == 0)
        {
            logger.LogDebug("[OutboxTimer] No stale Processing rows found.");
            return;
        }

        staleRows.ForEach(o => o.Status = "Pending");
        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "[OutboxTimer] Reset {Count} stale Processing row(s) back to Pending (threshold: {Threshold} min).",
            staleRows.Count, _outbox.StaleProcessingThresholdMinutes);

        telemetry.TrackEvent("OutboxStaleRowsReset", new Dictionary<string, string>
        {
            ["Count"] = staleRows.Count.ToString(),
            ["ThresholdMinutes"] = _outbox.StaleProcessingThresholdMinutes.ToString()
        });
    }

    private async Task PublishPendingBatchAsync(NotificationDbContext db, CancellationToken ct)
    {
        var batch = await db.NotificationOutbox
            .Where(o => o.Status == "Pending")
            .OrderBy(o => o.CreatedAt)
            .Take(_outbox.BatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
        {
            logger.LogDebug("[OutboxTimer] No pending outbox rows to publish.");
            return;
        }

        logger.LogInformation("[OutboxTimer] Claimed {Count} outbox row(s) for publishing.", batch.Count);

        batch.ForEach(o => o.Status = "Processing");
        await db.SaveChangesAsync(ct);

        var published = 0;
        var failed = 0;

        foreach (var row in batch)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                logger.LogDebug(
                    "[OutboxTimer] Publishing OutboxRow {OutboxId} → NotificationHistoryId {NotifId}.",
                    row.Id, row.NotificationHistoryId);

                var payload = JsonSerializer.Serialize(
                    new { NotificationHistoryId = row.NotificationHistoryId });

                var sbMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(payload))
                {
                    MessageId = row.Id.ToString(),
                    ContentType = "application/json"
                };

                await sbSender.SendMessageAsync(sbMessage, ct);
                sw.Stop();

                row.Status = "Published";
                row.ProcessedAt = DateTime.UtcNow;
                published++;

                logger.LogInformation(
                    "[OutboxTimer] Published NotificationHistoryId {NotifId} to Service Bus in {Ms}ms.",
                    row.NotificationHistoryId, sw.ElapsedMilliseconds);

                telemetry.TrackDependency("ServiceBus", "Queue", "Publish", true, sw.Elapsed);
                telemetry.TrackEvent("OutboxMessagePublished", new Dictionary<string, string>
                {
                    ["OutboxId"] = row.Id.ToString(),
                    ["NotificationHistoryId"] = row.NotificationHistoryId.ToString()
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                row.Status = "Failed";
                row.FailureReason = ex.Message;
                failed++;

                logger.LogError(ex,
                    "[OutboxTimer] Failed to publish OutboxRow {OutboxId} (NotifId {NotifId}) to Service Bus.",
                    row.Id, row.NotificationHistoryId);

                telemetry.TrackDependency("ServiceBus", "Queue", "Publish", false, sw.Elapsed);
                telemetry.TrackException(ex, new Dictionary<string, string>
                {
                    ["OutboxId"] = row.Id.ToString(),
                    ["NotificationHistoryId"] = row.NotificationHistoryId.ToString()
                });
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[OutboxTimer] Batch complete — Published: {Published}, Failed: {Failed}.",
            published, failed);

        telemetry.TrackEvent("OutboxBatchComplete", new Dictionary<string, string>
        {
            ["Published"] = published.ToString(),
            ["Failed"] = failed.ToString()
        });
    }
}
