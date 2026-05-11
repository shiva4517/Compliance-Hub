namespace ComplianceHub.Application.Common.Interfaces;

public interface ILlmClientFactory
{
    Task<ILlmClient> CreateForUserAsync(Guid securityUserId, CancellationToken ct);
}
