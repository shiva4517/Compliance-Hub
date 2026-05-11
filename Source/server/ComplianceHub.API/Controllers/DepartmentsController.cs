using ComplianceHub.API.Models;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Departments.Commands.CreateDepartment;
using ComplianceHub.Application.Features.Departments.Commands.ToggleDepartmentStatus;
using ComplianceHub.Application.Features.Departments.Commands.UpdateDepartment;
using ComplianceHub.Application.Features.Departments.Queries.GetDepartmentById;
using ComplianceHub.Application.Features.Departments.Queries.GetDepartments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize(Policy = "Admin")]
public class DepartmentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<DepartmentDto>>>> GetAll(
        [FromQuery] Guid companyId, [FromQuery] string? search,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetDepartmentsQuery(companyId, search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<DepartmentDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDepartmentByIdQuery(id), ct);
        return Ok(ApiResponse<DepartmentDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateDepartmentCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id },
            ApiResponse<Guid>.Ok(id, "Department created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateDepartmentCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Department updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(
        Guid id, [FromBody] ToggleStatusRequest body, CancellationToken ct)
    {
        await mediator.Send(new ToggleDepartmentStatusCommand(id, body.IsActive), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Department status updated."));
    }
}
