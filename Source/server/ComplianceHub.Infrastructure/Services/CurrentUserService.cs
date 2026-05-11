using System.Security.Claims;
using ComplianceHub.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ComplianceHub.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            // Prefer the domain entity ID sent explicitly by the client (Employee.Id, Customer.Id, etc.)
            var header = accessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
            if (Guid.TryParse(header, out var headerGuid)) return headerGuid;

            // Fall back to SecurityUser.Id from JWT (used when header is absent)
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public Guid? SecurityUserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
