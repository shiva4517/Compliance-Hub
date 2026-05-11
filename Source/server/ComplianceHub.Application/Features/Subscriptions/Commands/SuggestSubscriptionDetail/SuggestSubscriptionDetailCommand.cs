using System.Text.Json.Serialization;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Subscriptions.Commands.SuggestSubscriptionDetail;

public record SuggestSubscriptionDetailCommand(
    Guid SubscriptionId,
    string? Description,
    string? Condition,
    string? SuggestedTask,
    decimal? MinValue,
    decimal? MaxValue) : IRequest<SuggestSubscriptionDetailResponse>;

public record SuggestSubscriptionDetailResponse(
    string Description,
    string Condition,
    string SuggestedTask,
    string FrequencyType,
    string DueDateType,
    string Provider,
    // Force these to always appear in the JSON response (even when null), overriding
    // the global JsonIgnoreCondition.WhenWritingNull so the client and any HTTP
    // inspector can see exactly what the AI returned for the Data Range fields.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] decimal? MinValue,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] decimal? MaxValue);

public class SuggestSubscriptionDetailCommandHandler(
    IUnitOfWork uow,
    IRegulationsUnitOfWork regulationsUow,
    ICurrentUserService currentUser,
    IAiProviderSecretProtector protector,
    IAiSuggestionService aiSuggestionService)
    : IRequestHandler<SuggestSubscriptionDetailCommand, SuggestSubscriptionDetailResponse>
{
    public async Task<SuggestSubscriptionDetailResponse> Handle(SuggestSubscriptionDetailCommand request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
            throw new UnauthorizedAccessException("Authenticated user not found.");

        var subscription = await uow.Subscriptions.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, ct)
            ?? throw new KeyNotFoundException("Subscription not found.");

        var connection = await uow.AiProviderConnections.Query()
            .FirstOrDefaultAsync(x => x.SecurityUserId == securityUserId, ct)
            ?? throw new InvalidOperationException("No AI provider is connected for this admin user.");

        // For Regulation-level subscriptions we feed the actual section text to the AI.
        // For higher-level subscriptions (Entity/Agency/Category/Type/SubType) we only have
        // the node name as context — the AI prompt still produces sensible output.
        string titleName = "Federal Regulation";
        string sectionNumber = subscription.SubscribedNodeName;
        string sectionName = subscription.SubscribedNodeName;
        string? htmlContent = null;

        if (subscription.RegulationId is Guid regId)
        {
            var regulation = await regulationsUow.Regulations.Query()
                .Include(r => r.GovernmentEntity)
                .FirstOrDefaultAsync(r => r.Id == regId, ct);
            if (regulation is not null)
            {
                titleName = regulation.GovernmentEntity?.TitleName ?? titleName;
                sectionNumber = regulation.SectionNumber;
                sectionName = regulation.SectionName;
                htmlContent = regulation.HtmlContent;
            }
        }

        var suggestion = await aiSuggestionService.SuggestRegulationDetailAsync(
            new RegulationSuggestionContext(
                RegulationId: subscription.RegulationId ?? Guid.Empty,
                Provider: connection.Provider,
                TitleName: titleName,
                SectionNumber: sectionNumber,
                SectionName: sectionName,
                ExistingDescription: request.Description,
                ExistingCondition: request.Condition,
                ExistingSuggestedTask: request.SuggestedTask,
                HtmlContent: htmlContent,
                SubscribedNodeName: subscription.SubscribedNodeName,
                SubscribingLevel: subscription.SubscribingLevel.ToString(),
                ExistingMinValue: request.MinValue,
                ExistingMaxValue: request.MaxValue),
            new AiProviderConnectionSettings(
                connection.Provider,
                protector.Unprotect(connection.EncryptedApiKey),
                connection.Endpoint,
                connection.DeploymentName,
                connection.Model,
                connection.ApiVersion),
            ct);

        return new SuggestSubscriptionDetailResponse(
            suggestion.Description,
            suggestion.Condition,
            suggestion.SuggestedTask,
            suggestion.FrequencyType,
            suggestion.DueDateType,
            suggestion.Provider,
            suggestion.MinValue,
            suggestion.MaxValue);
    }
}
