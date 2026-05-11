using ComplianceHub.API.Models;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.CompanyDivisions.Commands.CreateCompanyDivision;
using ComplianceHub.Application.Features.CompanyDivisions.Commands.DeleteCompanyDivision;
using ComplianceHub.Application.Features.CompanyDivisions.Commands.ToggleCompanyDivisionStatus;
using ComplianceHub.Application.Features.CompanyDivisions.Commands.UpdateCompanyDivision;
using ComplianceHub.Application.Features.CompanyDivisions.Queries.GetCompanyDivisionById;
using ComplianceHub.Application.Features.CompanyDivisions.Queries.GetCompanyDivisions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/company-divisions")]
[Authorize]
public class CompanyDivisionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<CompanyDivisionDto>>>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] string? search,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCompanyDivisionsQuery(companyId, search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<CompanyDivisionDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDivisionDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCompanyDivisionByIdQuery(id), ct);
        return Ok(ApiResponse<CompanyDivisionDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateCompanyDivisionCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id },
            ApiResponse<Guid>.Ok(id, "Division created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateCompanyDivisionCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Division updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(
        Guid id, [FromBody] ToggleStatusRequest body, CancellationToken ct)
    {
        await mediator.Send(new ToggleCompanyDivisionStatusCommand(id, body.IsActive), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Division status updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCompanyDivisionCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Division deleted successfully."));
    }
}
