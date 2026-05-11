using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordCommandHandler(IUnitOfWork uow) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await uow.SecurityUsers.GetByIdAsync(request.UserId, ct);
        if (user == null) {
             var users = await uow.SecurityUsers.FindAsync(x => x.UserId == request.UserId, ct);
            user = users.FirstOrDefault();

        }
        if(user == null)
        {

        throw new NotFoundException(nameof(Domain.Entities.SecurityUser), request.UserId);
        }

        if (!user.IsActive)
            throw new UnauthorizedException("Account is inactive.");

        var currentHash = ComputeSha512(request.CurrentPassword);
        if (user.PasswordHash != currentHash)
            throw new UnauthorizedException("Current password is incorrect.");

        if (request.NewPassword.Length < 8)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "NewPassword", ["New password must be at least 8 characters."] }
            });

        user.PasswordHash = ComputeSha512(request.NewPassword);
        user.IsForcePasswordChange = false;
        user.UpdatedAt = DateTime.UtcNow;
        uow.SecurityUsers.Update(user);
        await uow.SaveChangesAsync(ct);
    }

    private static string ComputeSha512(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
