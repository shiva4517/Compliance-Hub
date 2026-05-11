using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Groups.Commands.CreateGroup;
using ComplianceHub.Application.Features.Groups.Commands.DeleteGroup;
using ComplianceHub.Application.Features.Groups.Commands.UpdateGroup;
using ComplianceHub.Application.Features.Groups.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class GroupsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GroupDto>>>> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetGroupsQuery(), ct);
        return Ok(ApiResponse<List<GroupDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateGroupCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return Ok(ApiResponse<Guid>.Ok(id, "Group created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateGroupCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Group updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteGroupCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Group deleted successfully."));
    }
}
