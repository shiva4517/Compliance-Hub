using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities.Regulations;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Application.Features.Regulations.Commands.ApplySimulatedRegulationChange;

public record ApplySimulatedRegulationChangeCommand(
    Guid RegulationId,
    string HtmlContent,
    DateOnly SimulatedDate)
    : IRequest<ApplySimulatedRegulationChangeResult>;

public record ApplySimulatedRegulationChangeResult(Guid RegulationId, int NewVersion);

public class ApplySimulatedRegulationChangeCommandHandler(
    IRegulationsUnitOfWork uow,
    IOutboxProcessor outboxProcessor,
    ILogger<ApplySimulatedRegulationChangeCommandHandler> logger)
    : IRequestHandler<ApplySimulatedRegulationChangeCommand, ApplySimulatedRegulationChangeResult>
{
    public async Task<ApplySimulatedRegulationChangeResult> Handle(
        ApplySimulatedRegulationChangeCommand request, CancellationToken ct)
    {
        var reg = await uow.Regulations.Query()
            .Include(r => r.GovernmentEntity)
            .FirstOrDefaultAsync(r => r.Id == request.RegulationId, ct)
            ?? throw new InvalidOperationException("Regulation not found.");

        var newHtml = request.HtmlContent ?? string.Empty;
        var newHash = ComputeHash(newHtml);
        var newAmendedDate = request.SimulatedDate;

        var changed =
            !string.Equals(reg.HtmlContent ?? string.Empty, newHtml, StringComparison.Ordinal) ||
            !string.Equals(reg.ContentHash ?? string.Empty, newHash, StringComparison.Ordinal) ||
            reg.LastAmendedDate != newAmendedDate;

        if (!changed)
            throw new InvalidOperationException("Nothing changed to update.");

        var simulatedUtc = request.SimulatedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var previousVersion = reg.Version;

        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            // Update the regulation row
            reg.HtmlContent = newHtml;
            reg.ContentHash = newHash;
            reg.LastAmendedDate = newAmendedDate;
            reg.Version = previousVersion + 1;
            reg.UpdatedAt = simulatedUtc;

            // Update the parent government entity timestamps to mirror the
            // simulated date so the title-level timeline reflects the change.
            if (reg.GovernmentEntity is not null)
            {
                var govt = reg.GovernmentEntity;
                govt.LastAmendedDate = newAmendedDate;
                govt.LastSyncedDate = simulatedUtc;
                govt.UpdatedAt = simulatedUtc;
            }

            // Enqueue OutboxEvent so the OutboxProcessor will create a
            // ChangeNotice and fan out NotificationHistory rows to matching
            // subscribers, just like a real sync.
            await uow.OutboxEvents.AddAsync(new OutboxEvent
            {
                EventType = "RegulationChanged",
                Payload = JsonSerializer.Serialize(new
                {
                    RegulationId = reg.Id,
                    GovernmentEntityId = reg.GovernmentEntityId,
                    reg.SectionNumber,
                    ChangeType = RegulationChangeType.Update.ToString(),
                    reg.Version,
                    // Marker for downstream processors: skip email send and
                    // Service Bus publish for simulated changes. ChangeNotice
                    // + NotificationHistory are still written for audit.
                    IsSimulated = true,
                }),
            }, innerCt);

            await uow.SaveChangesAsync(innerCt);
        }, ct);

        logger.LogInformation(
            "Simulated change saved for Regulation {RegId} (v{Prev}→v{New}). OutboxEvent enqueued — triggering notification fan-out.",
            reg.Id, previousVersion, reg.Version);

        // Run the outbox processor inline (mirrors the real sync orchestrator)
        // so simulate is end-to-end: ChangeNotice + NotificationHistory get
        // written immediately rather than waiting on the timer.
        try
        {
            await outboxProcessor.ProcessPendingAsync(ct);
        }
        catch (Exception ex)
        {
            // Don't fail the simulate save if downstream dispatch hiccups —
            // the OutboxEvent is persisted and the timer will retry it.
            logger.LogError(ex, "OutboxProcessor failed for simulated change on Regulation {RegId}. The event is queued and will be retried.", reg.Id);
        }

        return new ApplySimulatedRegulationChangeResult(reg.Id, reg.Version);
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..64];
    }
}
