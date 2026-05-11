using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Password,
    string? PhoneNumber,
    string? Title,
    UserRole Role,
    Guid? SecurityGroupId,
    Guid? UserId = null,
    Guid? RefId = null
) : IRequest<Guid>;

public class CreateUserCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IEmailService emailService,
    ILogger<CreateUserCommandHandler> logger)
    : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken ct)
    {
        //// For SuperAdmin/Admin (role-level users), enforce email uniqueness among that role
        //var exists = await uow.SecurityUsers.ExistsAsync(
        //    u => u.Email.ToLower() == request.Email.ToLower()
        //         && u.Role == request.Role
        //         && !u.IsDeleted, ct);
        //if (exists) throw new ConflictException($"A {request.Role} user with email '{request.Email}' already exists.");

        var groupId = request.SecurityGroupId;
        if (groupId is null)
        {
            var group = await uow.SecurityGroups.FindFirstAsync(g => g.Role == request.Role && !g.IsDeleted, ct);
            groupId = group?.Id;
        }

        var tempPassword = string.IsNullOrWhiteSpace(request.Password) ? GenerateTempPassword() : request.Password;

        var user = new SecurityUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = ComputeSha512(tempPassword),
            PhoneNumber = request.PhoneNumber,
            Title = request.Title ?? "Administrator",
            Role = request.Role,
            UserId = request.UserId,
            SecurityGroupId = groupId,
            RefId = request.RefId,
            IsForcePasswordChange = true,
            IsActive = true,
            CreatedBy = currentUser.Email
        };

        await uow.SecurityUsers.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        try
        {
            await emailService.SendWelcomeEmailAsync(user.Email, user.FullName, user.Email, tempPassword, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        return user.Id;
    }

    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%";
        const string all = upper + lower + digits + special;

        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[12];
        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];
        for (var i = 4; i < 12; i++)
            chars[i] = all[bytes[i] % all.Length];

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
    }

    internal static string ComputeSha512(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
