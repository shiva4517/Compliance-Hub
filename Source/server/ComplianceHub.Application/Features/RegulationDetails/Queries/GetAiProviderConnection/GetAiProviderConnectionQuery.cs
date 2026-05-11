using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.RegulationDetails;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.RegulationDetails.Queries.GetAiProviderConnection;

public record GetAiProviderConnectionQuery : IRequest<AiProviderConnectionViewDto?>;

public record AiProviderConnectionViewDto(
    string Provider,
    string ProviderDisplayName,
    string? Endpoint,
    string? DeploymentName,
    string? Model,
    string? ApiVersion,
    bool IsConfigured);

public class GetAiProviderConnectionQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAiProviderConnectionQuery, AiProviderConnectionViewDto?>
{
    public async Task<AiProviderConnectionViewDto?> Handle(GetAiProviderConnectionQuery request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
        {
            throw new UnauthorizedAccessException("Authenticated user not found.");
        }

        var connection = await uow.AiProviderConnections.Query()
            .FirstOrDefaultAsync(x => x.SecurityUserId == securityUserId, ct);

        if (connection is null)
        {
            return null;
        }

        return new AiProviderConnectionViewDto(
            connection.Provider,
            AiProviderMetadata.GetProviderDisplayName(connection.Provider),
            connection.Endpoint,
            connection.DeploymentName,
            connection.Model,
            connection.ApiVersion,
            true);
    }
}
