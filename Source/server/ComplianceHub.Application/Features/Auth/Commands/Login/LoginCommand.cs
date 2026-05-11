using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string FullName,
    string Role,
    Guid UserId,
    bool IsForcePasswordChange
);

public class LoginCommandHandler(IUnitOfWork uow, IJwtService jwtService) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await uow.SecurityUsers.FindFirstAsync(
            u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, ct)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is inactive.");

        var hash = ComputeSha512(request.Password);
        if (user.PasswordHash != hash)
            throw new UnauthorizedException("Invalid email or password.");

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;
        uow.SecurityUsers.Update(user);
        await uow.SaveChangesAsync(ct);

        // userId in the response is the polymorphic domain entity ID:
        //   Admin    → Company.Id  (SecurityUser.UserId)
        //   Customer → Customer.Id (SecurityUser.UserId)
        //   Employee → Employee.Id (SecurityUser.UserId)
        //   SuperAdmin → SecurityUser.Id (no domain entity)
        var entityId = user.UserId ?? user.Id;
        return new LoginResponse(accessToken, refreshToken, user.Email, user.FullName, user.Role.ToString(), entityId, user.IsForcePasswordChange);
    }

    private static string ComputeSha512(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
