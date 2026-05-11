using System.Text.Json.Serialization;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Subscriptions.Queries.GetSubscriptionDetail;

public record GetSubscriptionDetailQuery(Guid SubscriptionId) : IRequest<SubscriptionDetailDto?>;

public record SubscriptionDetailDto(
    Guid? Id,
    Guid SubscriptionId,
    string? SubscribingLevel,
    string? Description,
    string? Condition,
    string? SuggestedTask,
    Guid? FrequencyTypeId,
    string? FrequencyTypeName,
    Guid? DueDateTypeId,
    string? DueDateTypeName,
    // Force Data Range fields to always appear in the JSON response, even when null.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] decimal? MinValue,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] decimal? MaxValue);

public class GetSubscriptionDetailQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetSubscriptionDetailQuery, SubscriptionDetailDto?>
{
    public async Task<SubscriptionDetailDto?> Handle(GetSubscriptionDetailQuery request, CancellationToken ct)
    {
        var detail = await uow.RegulationDetails.Query()
            .Include(d => d.FrequencyType)
            .Include(d => d.DueDateType)
            .FirstOrDefaultAsync(d => d.SubscriptionId == request.SubscriptionId, ct);

        if (detail is null) return null;

        return new SubscriptionDetailDto(
            detail.Id,
            request.SubscriptionId,
            detail.SubscribingLevel?.ToString(),
            detail.Description,
            detail.Condition,
            detail.SuggestedTask,
            detail.FrequencyTypeId,
            detail.FrequencyType?.Name,
            detail.DueDateTypeId,
            detail.DueDateType?.Name,
            detail.MinValue,
            detail.MaxValue);
    }
}
