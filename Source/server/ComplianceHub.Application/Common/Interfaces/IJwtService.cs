using ComplianceHub.Domain.Entities;

namespace ComplianceHub.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(SecurityUser user);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
