using ComplianceHub.Application.Features.Agent.Models;

namespace ComplianceHub.Application.Common.Interfaces;

public interface ILlmClient
{
    Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct);
}
