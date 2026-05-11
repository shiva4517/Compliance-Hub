using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class SecurityGroupConfiguration : IEntityTypeConfiguration<SecurityGroup>
{
    public void Configure(EntityTypeBuilder<SecurityGroup> builder)
    {
        builder.ToTable("SecurityGroups");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.GroupName).IsRequired().HasMaxLength(100);
        builder.HasIndex(e => e.GroupName).IsUnique();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Role).IsRequired();
    }
}
