using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class SecurityUserConfiguration : IEntityTypeConfiguration<SecurityUser>
{
    public void Configure(EntityTypeBuilder<SecurityUser> builder)
    {
        builder.ToTable("SecurityUsers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(e => e.Email); // not unique — same email allowed across companies
        builder.Property(e => e.PasswordHash).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(20);
        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.Role).IsRequired();
        builder.Property(e => e.IsForcePasswordChange).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.RefId);
        builder.Property(e => e.UserId);

        builder.HasOne(e => e.SecurityGroup)
            .WithMany(g => g.Users)
            .HasForeignKey(e => e.SecurityGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
