using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Notifications.Commands.MarkNotificationsSeen;
using ComplianceHub.Application.Features.Notifications.Commands.RetryNotification;
using ComplianceHub.Application.Features.Notifications.Queries.GetNotificationById;
using ComplianceHub.Application.Features.Notifications.Queries.GetNotifications;
using ComplianceHub.Application.Features.Notifications.Queries.GetUnseenCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Admin")]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet("unseen-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnseenCount(
        [FromQuery] Guid companyId,
        CancellationToken ct = default)
    {
        var count = await mediator.Send(new GetUnseenCountQuery(companyId), ct);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPost("mark-seen")]
    public async Task<IActionResult> MarkSeen(
        [FromQuery] Guid companyId,
        CancellationToken ct = default)
    {
        await mediator.Send(new MarkNotificationsSeenCommand(companyId), ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<NotificationLogDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetNotificationsQuery(companyId, search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<NotificationLogDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationLogDetailDto>>> GetById(
        Guid id,
        [FromQuery] Guid companyId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new GetNotificationByIdQuery(id, companyId), ct);
            if (result is null) return NotFound(ApiResponse<object>.Fail("Notification not found."));
            return Ok(ApiResponse<NotificationLogDetailDto>.Ok(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<ApiResponse<RetryNotificationResult>>> Retry(
        Guid id,
        [FromQuery] Guid companyId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new RetryNotificationCommand(id, companyId), ct);
            return Ok(ApiResponse<RetryNotificationResult>.Ok(result, result.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<RetryNotificationResult>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<RetryNotificationResult>.Fail(ex.Message));
        }
    }
}
