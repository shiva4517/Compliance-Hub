namespace ComplianceHub.Application.Common.Interfaces;

public interface IOutboxProcessor
{
    Task ProcessPendingAsync(CancellationToken ct = default);
}
