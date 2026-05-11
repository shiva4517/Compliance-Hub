using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using MediatR;

namespace ComplianceHub.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<RefreshTokenResponse>;
public record RefreshTokenResponse(string AccessToken, string RefreshToken);

public class RefreshTokenCommandHandler(IUnitOfWork uow, IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var principal = jwtService.GetPrincipalFromExpiredToken(request.AccessToken)
            ?? throw new UnauthorizedException("Invalid token.");

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            throw new UnauthorizedException("Invalid token.");

        var user = await uow.SecurityUsers.GetByIdAsync(id, ct)
            ?? throw new UnauthorizedException("User not found.");

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var newAccess = jwtService.GenerateAccessToken(user);
        var newRefresh = jwtService.GenerateRefreshToken();
        user.RefreshToken = newRefresh;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        uow.SecurityUsers.Update(user);
        await uow.SaveChangesAsync(ct);

        return new RefreshTokenResponse(newAccess, newRefresh);
    }
}
