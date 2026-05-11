using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.RegulationDetails.Commands.SuggestRegulationDetail;

public record SuggestRegulationDetailCommand(
    Guid RegulationId,
    string? Description,
    string? Condition,
    string? SuggestedTask) : IRequest<SuggestRegulationDetailResponse>;

public record SuggestRegulationDetailResponse(
    string Description,
    string Condition,
    string SuggestedTask,
    string FrequencyType,
    string DueDateType,
    string Provider);

public class SuggestRegulationDetailCommandHandler(
    IRegulationsUnitOfWork regulationsUow,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IAiProviderSecretProtector protector,
    IAiSuggestionService aiSuggestionService)
    : IRequestHandler<SuggestRegulationDetailCommand, SuggestRegulationDetailResponse>
{
    public async Task<SuggestRegulationDetailResponse> Handle(SuggestRegulationDetailCommand request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
        {
            throw new UnauthorizedAccessException("Authenticated user not found.");
        }

        var regulation = await regulationsUow.Regulations.Query()
            .Include(r => r.GovernmentEntity)
            .FirstOrDefaultAsync(r => r.Id == request.RegulationId, ct);

        if (regulation is null)
        {
            throw new KeyNotFoundException("Regulation section not found.");
        }

        var connection = await uow.AiProviderConnections.Query()
            .FirstOrDefaultAsync(x => x.SecurityUserId == securityUserId, ct);

        if (connection is null)
        {
            throw new InvalidOperationException("No AI provider is connected for this admin user.");
        }

        var suggestion = await aiSuggestionService.SuggestRegulationDetailAsync(
            new RegulationSuggestionContext(
                RegulationId: request.RegulationId,
                Provider: connection.Provider,
                TitleName: regulation.GovernmentEntity?.TitleName ?? "Federal Regulation",
                SectionNumber: regulation.SectionNumber,
                SectionName: regulation.SectionName,
                ExistingDescription: request.Description,
                ExistingCondition: request.Condition,
                ExistingSuggestedTask: request.SuggestedTask,
                HtmlContent: regulation.HtmlContent),
            new AiProviderConnectionSettings(
                connection.Provider,
                protector.Unprotect(connection.EncryptedApiKey),
                connection.Endpoint,
                connection.DeploymentName,
                connection.Model,
                connection.ApiVersion),
            ct);

        return new SuggestRegulationDetailResponse(
            suggestion.Description,
            suggestion.Condition,
            suggestion.SuggestedTask,
            suggestion.FrequencyType,
            suggestion.DueDateType,
            suggestion.Provider);
    }
}
