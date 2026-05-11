using ComplianceHub.API.Models;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Districts.Commands.CreateDistrict;
using ComplianceHub.Application.Features.Districts.Commands.ToggleDistrictStatus;
using ComplianceHub.Application.Features.Districts.Commands.UpdateDistrict;
using ComplianceHub.Application.Features.Districts.Queries.GetDistrictById;
using ComplianceHub.Application.Features.Districts.Queries.GetDistricts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/districts")]
[Authorize(Policy = "Admin")]
public class DistrictsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<DistrictDto>>>> GetAll(
        [FromQuery] Guid companyId, [FromQuery] string? search,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetDistrictsQuery(companyId, search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<DistrictDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DistrictDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDistrictByIdQuery(id), ct);
        return Ok(ApiResponse<DistrictDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateDistrictCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id },
            ApiResponse<Guid>.Ok(id, "District created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateDistrictCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "District updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(
        Guid id, [FromBody] ToggleStatusRequest body, CancellationToken ct)
    {
        await mediator.Send(new ToggleDistrictStatusCommand(id, body.IsActive), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "District status updated."));
    }
}
