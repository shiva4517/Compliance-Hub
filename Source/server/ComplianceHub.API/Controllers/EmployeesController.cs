using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Employees.Commands.AssignWork;
using ComplianceHub.Application.Features.Employees.Commands.CreateEmployee;
using ComplianceHub.Application.Features.Employees.Commands.DeleteAssignedWork;
using ComplianceHub.Application.Features.Employees.Commands.DeleteEmployee;
using ComplianceHub.Application.Features.Employees.Commands.RestoreEmployee;
using ComplianceHub.Application.Features.Employees.Commands.UpdateEmployee;
using ComplianceHub.Application.Features.Employees.Queries.GetAssignedWorkById;
using ComplianceHub.Application.Features.Employees.Queries.GetAvailableSubscriptionsForEmployee;
using ComplianceHub.Application.Features.Employees.Queries.GetEmployeeAssignedWork;
using ComplianceHub.Application.Features.Employees.Queries.GetEmployeeById;
using ComplianceHub.Application.Features.Employees.Queries.GetEmployeeChangeNotices;
using ComplianceHub.Application.Features.Employees.Queries.GetEmployees;
using ComplianceHub.Application.Features.Employees.Queries.GetMyAssignmentDetail;
using ComplianceHub.Application.Features.Employees.Queries.GetMyProfile;
using ComplianceHub.Application.Features.Employees.Queries.GetMyStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    // ── Employee self-service (/me) ───────────────────────────────────────────

    [HttpGet("me")]
    [Authorize(Policy = "Employee")]
    public async Task<ActionResult<ApiResponse<MyProfileDto>>> GetMyProfile(CancellationToken ct)
    {
        var employeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await mediator.Send(new GetMyProfileQuery(employeeId), ct);
        return Ok(ApiResponse<MyProfileDto>.Ok(result));
    }

    [HttpGet("me/stats")]
    [Authorize(Policy = "Employee")]
    public async Task<ActionResult<ApiResponse<MyStatsDto>>> GetMyStats(CancellationToken ct)
    {
        var employeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await mediator.Send(new GetMyStatsQuery(employeeId), ct);
        return Ok(ApiResponse<MyStatsDto>.Ok(result));
    }

    [HttpGet("me/assigned-work")]
    [Authorize(Policy = "Employee")]
    public async Task<ActionResult<ApiResponse<List<AssignedWorkDto>>>> GetMyAssignedWork(CancellationToken ct)
    {
        var employeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await mediator.Send(new GetEmployeeAssignedWorkQuery(employeeId), ct);
        return Ok(ApiResponse<List<AssignedWorkDto>>.Ok(result));
    }

    [HttpGet("me/assigned-work/{assignmentId:guid}")]
    [Authorize(Policy = "Employee")]
    public async Task<ActionResult<ApiResponse<MyAssignmentDetailDto>>> GetMyAssignmentDetail(
        Guid assignmentId, CancellationToken ct)
    {
        var employeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await mediator.Send(new GetMyAssignmentDetailQuery(assignmentId, employeeId), ct);
        return Ok(ApiResponse<MyAssignmentDetailDto>.Ok(result));
    }

    [HttpGet("me/change-notices")]
    [Authorize(Policy = "Employee")]
    public async Task<ActionResult<ApiResponse<PaginatedList<EmployeeChangeNoticeDto>>>> GetMyChangeNotices(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var employeeId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await mediator.Send(
            new GetEmployeeChangeNoticesQuery(employeeId, fromDate, toDate, search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<EmployeeChangeNoticeDto>>.Ok(result));
    }


    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<EmployeeDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetEmployeesQuery(search, companyId, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<EmployeeDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEmployeeByIdQuery(id), ct);
        return Ok(ApiResponse<EmployeeDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<CreateEmployeeResponse>>> Create(
        [FromBody] CreateEmployeeCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.EmployeeId },
            ApiResponse<CreateEmployeeResponse>.Ok(result, "Employee created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateEmployeeCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee updated successfully."));
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Restore(
        Guid id, [FromBody] RestoreEmployeeCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        var code = await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { employeeCode = code }, "Employee restored successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteEmployeeCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Employee deleted successfully."));
    }

    [HttpGet("{id:guid}/assigned-work")]
    public async Task<ActionResult<ApiResponse<List<AssignedWorkDto>>>> GetAssignedWork(
        Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEmployeeAssignedWorkQuery(id), ct);
        return Ok(ApiResponse<List<AssignedWorkDto>>.Ok(result));
    }

    [HttpGet("assigned-work/{assignmentId:guid}")]
    public async Task<ActionResult<ApiResponse<AssignedWorkDto>>> GetAssignedWorkById(
        Guid assignmentId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAssignedWorkByIdQuery(assignmentId), ct);
        return Ok(ApiResponse<AssignedWorkDto>.Ok(result));
    }

    [HttpGet("{id:guid}/available-subscriptions")]
    public async Task<ActionResult<ApiResponse<List<AvailableSubscriptionDto>>>> GetAvailableSubscriptions(
        Guid id, [FromQuery] Guid customerId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAvailableSubscriptionsForEmployeeQuery(id, customerId), ct);
        return Ok(ApiResponse<List<AvailableSubscriptionDto>>.Ok(result));
    }

    [HttpPost("{id:guid}/assign-work")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> AssignWork(
        Guid id, [FromBody] AssignWorkRequest body, CancellationToken ct)
    {
        var assignmentId = await mediator.Send(new AssignWorkCommand(id, body.SubscriptionId, body.CustomerId), ct);
        return Ok(ApiResponse<Guid>.Ok(assignmentId, "Work assigned successfully."));
    }

    [HttpDelete("assigned-work/{assignmentId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAssignedWork(
        Guid assignmentId, CancellationToken ct)
    {
        await mediator.Send(new DeleteAssignedWorkCommand(assignmentId), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Assignment removed."));
    }
}

public record AssignWorkRequest(Guid SubscriptionId, Guid CustomerId);
