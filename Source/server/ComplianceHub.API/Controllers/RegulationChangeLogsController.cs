using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.RegulationChangeLogs.Queries.GetRegulationChangeLogById;
using ComplianceHub.Application.Features.RegulationChangeLogs.Queries.GetRegulationChangeLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/regulation-change-logs")]
[Authorize]
public class RegulationChangeLogsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<RegulationChangeLogDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string sortBy = "ChangedAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetRegulationChangeLogsQuery(search, fromDate, toDate, sortBy, sortDir, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<RegulationChangeLogDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RegulationChangeLogDetailDto>>> GetById(
        Guid id,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetRegulationChangeLogByIdQuery(id), ct);
        if (result is null) return NotFound(ApiResponse<object>.Fail("Change log not found."));
        return Ok(ApiResponse<RegulationChangeLogDetailDto>.Ok(result));
    }
}
