using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Users.Commands.CreateUser;
using ComplianceHub.Application.Features.Users.Commands.UpdateUser;
using ComplianceHub.Application.Features.Users.Queries.GetUserById;
using ComplianceHub.Application.Features.Users.Queries.GetUsers;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<UserDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] UserRole? role,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsersQuery(search, role, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<UserDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), ct);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateUserCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.Ok(id, "User created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateUserCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(null!, "User updated successfully."));
    }
}
