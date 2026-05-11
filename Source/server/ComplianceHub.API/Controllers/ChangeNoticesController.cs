using ComplianceHub.API.Filters;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.ChangeNotices.Queries.GetChangeNotices;
using ComplianceHub.Application.Features.ChangeNotices.Queries.GetNotificationHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/change-notices")]
[Authorize]
public class ChangeNoticesController(IMediator mediator, IOutboxProcessor outboxProcessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<ChangeNoticeDto>>>> GetAll(
        [FromQuery] string? role,
        [FromQuery] Guid? referenceId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetChangeNoticesQuery(role, referenceId, fromDate, toDate, search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<ChangeNoticeDto>>.Ok(result));
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<PaginatedList<NotificationHistoryDto>>>> GetNotifications(
        [FromQuery] Guid? customerId, [FromQuery] Guid? companyId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetNotificationHistoryQuery(customerId, companyId, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<NotificationHistoryDto>>.Ok(result));
    }

    [HttpPost("process-outbox")]
    [AllowAnonymous]
    [InternalApiKey]
    public async Task<IActionResult> ProcessOutbox(CancellationToken ct)
    {
        await outboxProcessor.ProcessPendingAsync(ct);
        return Ok();
    }
}
