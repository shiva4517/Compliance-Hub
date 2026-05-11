using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Auth.Commands.ChangePassword;
using ComplianceHub.Application.Features.Auth.Commands.Login;
using ComplianceHub.Application.Features.Auth.Commands.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<RefreshTokenResponse>.Ok(result));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId)
            return Unauthorized();
        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Password changed successfully."));
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
