using System.Text.Json;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using ComplianceHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Infrastructure.Services.Sync;

public class OutboxProcessor(
    IRegulationsUnitOfWork regulationsUow,
    IUnitOfWork uow,
    ComplianceHubDbContext portalDb,
    IEmailService emailService,
    IConfiguration config,
    ILogger<OutboxProcessor> logger) : IOutboxProcessor
{
    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        var pendingEvents = await regulationsUow.OutboxEvents.Query()
            .Where(e => e.ProcessedAt == null && e.RetryCount < 5)
            .OrderBy(e => e.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        logger.LogInformation("Processing {Count} outbox events", pendingEvents.Count);

        foreach (var outboxEvent in pendingEvents)
        {
            try
            {
                await ProcessEventAsync(outboxEvent, ct);
                outboxEvent.ProcessedAt = DateTime.UtcNow;
                regulationsUow.OutboxEvents.Update(outboxEvent);
                await regulationsUow.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox event {Id}", outboxEvent.Id);
                outboxEvent.RetryCount++;
                regulationsUow.OutboxEvents.Update(outboxEvent);
                await regulationsUow.SaveChangesAsync(ct);
            }
        }
    }

    private async Task ProcessEventAsync(Domain.Entities.Regulations.OutboxEvent outboxEvent, CancellationToken ct)
    {
        if (outboxEvent.EventType != "RegulationChanged") return;

        var payload = JsonSerializer.Deserialize<RegulationChangedPayload>(outboxEvent.Payload);
        if (payload is null) return;

        // Load regulation hierarchy for the changed regulation
        var regulation = await regulationsUow.Regulations.Query()
            .Include(r => r.Agency)
            .Include(r => r.RegulationCategory)
            .Include(r => r.RegulationType)
            .Include(r => r.RegulationSubtype)
            .Include(r => r.GovernmentEntity)
            .FirstOrDefaultAsync(r => r.Id == payload.RegulationId, ct);

        // Load previous version from change history
        var prevHistory = await regulationsUow.RegulationChangeHistory.Query()
            .Where(h => h.RegulationId == payload.RegulationId && h.Version == payload.Version - 1)
            .OrderByDescending(h => h.Version)
            .FirstOrDefaultAsync(ct);

        var changeNotice = new ChangeNotice
        {
            RegulationId = payload.RegulationId,
            GovernmentEntityId = payload.GovernmentEntityId,
            GovernmentEntityName = regulation?.GovernmentEntity?.TitleName ?? string.Empty,
            AgencyId = regulation?.AgencyId,
            AgencyName = regulation?.Agency?.AgencyName ?? string.Empty,
            RegulationCategoryId = regulation?.RegulationCategoryId,
            RegulationCategoryName = regulation?.RegulationCategory?.SubchapterName ?? string.Empty,
            RegulationTypeId = regulation?.RegulationTypeId,
            RegulationTypeName = regulation?.RegulationType?.PartName ?? string.Empty,
            RegulationSubtypeId = regulation?.RegulationSubtypeId,
            RegulationSubtypeName = regulation?.RegulationSubtype?.SubpartName,
            SectionNumber = regulation?.SectionNumber ?? payload.SectionNumber,
            SectionTitle = regulation?.SectionName ?? string.Empty,
            PreviousContentHash = prevHistory?.ContentHash,
            NewContentHash = regulation?.ContentHash,
            PreviousHtmlContent = prevHistory?.HtmlContent,
            NewHtmlContent = regulation?.HtmlContent,
            PreviousAmendedDate = null,
            NewAmendedDate = regulation?.LastAmendedDate,
            ArchivedVersion = payload.Version - 1,
            NewVersion = payload.Version,
            Version = payload.Version,
            ChangedAt = DateTime.UtcNow,
            CreatedBy = "SYSTEM",
        };

        await uow.ChangeNotices.AddAsync(changeNotice);
        await uow.SaveChangesAsync(ct);

        var senderEmail = config["Email:FromAddress"] ?? "";
        var senderName = config["Email:FromName"] ?? "Compliance Hub";

        // Match the regulation against every subscription level using the regulation's
        // full ancestry. The previous filter only checked GovernmentEntityId / RegulationId
        // and would miss any Agency / Category / Type / SubType-level subscription whose
        // GovernmentEntityId field was different (e.g., reseeded title id).
        // IgnoreQueryFilters() so notification dispatch is not silently dropped by
        // soft-delete filters on Subscription or Customer.
        var regGovernmentEntityId = regulation?.GovernmentEntityId ?? payload.GovernmentEntityId;
        var regAgencyId = regulation?.AgencyId;
        var regCategoryId = regulation?.RegulationCategoryId;
        var regTypeId = regulation?.RegulationTypeId;
        var regSubtypeId = regulation?.RegulationSubtypeId;
        var regId = payload.RegulationId;

        var subscriptions = await uow.Subscriptions.Query()
            .IgnoreQueryFilters()
            .Include(s => s.Customer)
            .Where(s => s.IsActive && !s.IsDeleted && (
                (s.SubscribingLevel == SubscribingLevel.Entity     && s.GovernmentEntityId   == regGovernmentEntityId) ||
                (s.SubscribingLevel == SubscribingLevel.Agency     && s.AgencyId             == regAgencyId) ||
                (s.SubscribingLevel == SubscribingLevel.Category   && s.RegulationCategoryId == regCategoryId) ||
                (s.SubscribingLevel == SubscribingLevel.Type       && s.RegulationTypeId     == regTypeId) ||
                (s.SubscribingLevel == SubscribingLevel.SubType    && s.RegulationSubtypeId  != null && s.RegulationSubtypeId == regSubtypeId) ||
                (s.SubscribingLevel == SubscribingLevel.Regulation && s.RegulationId         == regId)))
            .ToListAsync(ct);

        logger.LogInformation(
            "OutboxProcessor matched {Count} subscription(s) for RegulationId={RegId} (Title={TitleId}, Agency={AgencyId}, Category={CategoryId}, Type={TypeId}, SubType={SubtypeId})",
            subscriptions.Count, regId, regGovernmentEntityId, regAgencyId, regCategoryId, regTypeId, regSubtypeId);

        foreach (var sub in subscriptions)
        {
            var customer = sub.Customer;
            if (customer is null)
            {
                logger.LogWarning(
                    "Skipping subscription {SubId} ({Level}) — customer {CustomerId} not loaded (deleted or filter dropped).",
                    sub.Id, sub.SubscribingLevel, sub.CustomerId);
                continue;
            }
            if (customer.IsDeleted)
            {
                logger.LogWarning(
                    "Skipping subscription {SubId} — customer {CustomerId} is soft-deleted.",
                    sub.Id, sub.CustomerId);
                continue;
            }

            var (subject, body) = emailService.BuildRegulationChangeEmail(
                recipientName: customer.FullContactName,
                customerName: customer.CustomerName,
                governmentEntityName: regulation?.GovernmentEntity?.TitleName ?? payload.GovernmentEntityId.ToString(),
                previousRegulationName: prevHistory?.SectionNumber ?? "N/A",
                presentRegulationName: regulation?.SectionNumber ?? payload.SectionNumber,
                changeType: payload.ChangeType,
                regulationDetailsHtml: regulation?.HtmlContent);

            bool sent = false;
            string? failureReason = null;
            if (payload.IsSimulated)
            {
                // Simulated changes write audit rows but never send emails.
                failureReason = "Simulated change — email send skipped.";
                logger.LogInformation(
                    "Simulated change — skipping regulation change email to {Email}.", customer.PrimaryEmail);
            }
            else
            {
                try
                {
                    await emailService.SendRawEmailAsync(customer.PrimaryEmail, subject, body, ct);
                    sent = true;
                    logger.LogInformation("Regulation change email sent to {Email}", customer.PrimaryEmail);
                }
                catch (Exception ex)
                {
                    failureReason = ex.Message;
                    logger.LogWarning(ex, "Failed to send regulation change email to {Email}", customer.PrimaryEmail);
                }
            }

            var notification = new NotificationHistory
            {
                SubscriptionId = sub.Id,
                CustomerId = sub.CustomerId,
                CompanyId = customer.CompanyId,
                GovernmentEntityId = regulation?.GovernmentEntityId ?? payload.GovernmentEntityId,
                GovernmentEntityName = regulation?.GovernmentEntity?.TitleName,
                AgencyId = regulation?.AgencyId,
                AgencyName = regulation?.Agency?.AgencyName,
                RegulationCategoryId = regulation?.RegulationCategoryId,
                RegulationCategoryName = regulation?.RegulationCategory?.SubchapterName,
                RegulationTypeId = regulation?.RegulationTypeId,
                RegulationTypeName = regulation?.RegulationType?.PartName,
                RegulationSubtypeId = regulation?.RegulationSubtypeId,
                RegulationSubtypeName = regulation?.RegulationSubtype?.SubpartName,
                PreviousRegulationId = prevHistory?.Id,
                PreviousRegulationName = prevHistory?.SectionNumber,
                PresentRegulationId = regulation?.Id,
                PresentRegulationName = regulation?.SectionNumber ?? payload.SectionNumber,
                Subject = subject,
                Body = body,
                SenderEmail = senderEmail,
                SenderName = senderName,
                RecipientEmail = customer.PrimaryEmail,
                RecipientName = customer.FullContactName,
                IsNotified = sent,
                Status = payload.IsSimulated ? "Simulated" : (sent ? "Sent" : "Failed"),
                FailureReason = failureReason,
            };

            await uow.NotificationHistory.AddAsync(notification);

            var notificationSentHistory = new NotificationSentHistory
            {
                NotificationHistoryId = notification.Id,
                AttemptNumber = 1,
                IsSuccess = notification.IsNotified,
                Status = notification.Status,
                FailureReason = notification.FailureReason
            };
            await uow.NotificationSentHistories.AddAsync(notificationSentHistory);
        }

        if (subscriptions.Count > 0)
        {
            await uow.SaveChangesAsync(ct);

            // The DB trigger on NotificationHistory inserts a Pending row into
            // NotificationOutbox for each notification. For simulated changes,
            // re-mark those rows so the timer-based publisher skips them and
            // no Service Bus message (and therefore no email) is dispatched.
            if (payload.IsSimulated)
            {
                var updated = await portalDb.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE "NotificationOutbox"
                    SET "Status" = 'Simulated', "ProcessedAt" = NOW() AT TIME ZONE 'UTC'
                    WHERE "Status" = 'Pending'
                      AND "NotificationHistoryId" IN (
                          SELECT "Id" FROM "NotificationHistory"
                          WHERE "Status" = 'Simulated'
                      )
                    """, ct);
                logger.LogInformation(
                    "Simulated change — marked {Count} NotificationOutbox row(s) as Simulated (no Service Bus publish).",
                    updated);
            }
        }
    }

    private record RegulationChangedPayload(
        Guid RegulationId,
        Guid GovernmentEntityId,
        string SectionNumber,
        string ChangeType,
        int Version,
        bool IsSimulated = false);
}
