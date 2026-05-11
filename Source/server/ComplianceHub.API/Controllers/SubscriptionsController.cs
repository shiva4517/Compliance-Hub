using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Subscriptions.Commands.CreateSubscription;
using ComplianceHub.Application.Features.Subscriptions.Commands.DeleteSubscription;
using ComplianceHub.Application.Features.Subscriptions.Commands.SuggestSubscriptionDetail;
using ComplianceHub.Application.Features.Subscriptions.Commands.UpsertSubscriptionDetail;
using ComplianceHub.Application.Features.Subscriptions.Queries.GetSubscriptionDetail;
using ComplianceHub.Application.Features.Subscriptions.Queries.GetSubscriptionDetails;
using ComplianceHub.Application.Features.Subscriptions.Queries.GetSubscriptions;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<SubscriptionDto>>>> GetAll(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var role = currentUser.Role;

        Guid? resolvedCustomerId = null;
        Guid? resolvedCompanyId = null;

        if (role == "Customer")
        {
            resolvedCustomerId = currentUser.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated.");
        }
        else if (role == "Admin")
        {
            resolvedCompanyId = companyId ?? currentUser.UserId
                ?? throw new UnauthorizedAccessException("User not authenticated.");
        }
        else
        {
            // SuperAdmin or Employee: honour explicit params if provided
            resolvedCustomerId = customerId;
            resolvedCompanyId = companyId;
        }

        var result = await mediator.Send(new GetSubscriptionsQuery(resolvedCustomerId, resolvedCompanyId, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<SubscriptionDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateSubscriptionsResult>>> Create(
        [FromBody] CreateSubscriptionsCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<CreateSubscriptionsResult>.Ok(result,
            result.Saved > 0 ? $"{result.Saved} subscription(s) saved." : "No new subscriptions saved."));
    }

    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<ApiResponse<SubscriptionDetailsDto>>> GetDetails(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSubscriptionDetailsQuery(id), ct);
        if (result is null) return NotFound(ApiResponse<SubscriptionDetailsDto>.Fail("Subscription not found."));
        return Ok(ApiResponse<SubscriptionDetailsDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteSubscriptionCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Subscription deleted."));
    }

    // Subscription-scoped detail (Description / Condition / Suggested Task /
    // Frequency / DueDate / Min / Max). Backed by the RegulationDetails table
    // with SubscriptionId as the scope key.

    [HttpGet("{id:guid}/detail")]
    public async Task<ActionResult<ApiResponse<SubscriptionDetailDto?>>> GetDetail(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSubscriptionDetailQuery(id), ct);
        return Ok(ApiResponse<SubscriptionDetailDto?>.Ok(result));
    }

    [HttpPut("{id:guid}/detail")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpsertDetail(
        Guid id,
        [FromBody] UpsertSubscriptionDetailBody body,
        CancellationToken ct)
    {
        var detailId = await mediator.Send(new UpsertSubscriptionDetailCommand(
            id,
            body.Description,
            body.Condition,
            body.SuggestedTask,
            body.FrequencyTypeId,
            body.DueDateTypeId,
            body.MinValue,
            body.MaxValue), ct);
        return Ok(ApiResponse<Guid>.Ok(detailId, "Subscription detail saved."));
    }

    [HttpPost("{id:guid}/detail/suggest")]
    public async Task<ActionResult<ApiResponse<SuggestSubscriptionDetailResponse>>> SuggestDetail(
        Guid id,
        [FromBody] SuggestSubscriptionDetailBody body,
        CancellationToken ct)
    {
        var result = await mediator.Send(new SuggestSubscriptionDetailCommand(
            id, body.Description, body.Condition, body.SuggestedTask, body.MinValue, body.MaxValue), ct);
        return Ok(ApiResponse<SuggestSubscriptionDetailResponse>.Ok(result));
    }
}

public record UpsertSubscriptionDetailBody(
    string? Description,
    string? Condition,
    string? SuggestedTask,
    Guid? FrequencyTypeId,
    Guid? DueDateTypeId,
    decimal? MinValue,
    decimal? MaxValue);

public record SuggestSubscriptionDetailBody(
    string? Description,
    string? Condition,
    string? SuggestedTask,
    decimal? MinValue,
    decimal? MaxValue);
