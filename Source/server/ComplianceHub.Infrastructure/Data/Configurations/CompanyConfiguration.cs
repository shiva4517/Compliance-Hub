using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompanyCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.PrimaryEmail).IsRequired().HasMaxLength(200);
        builder.Property(e => e.SecondaryEmail).HasMaxLength(200);
        builder.Property(e => e.PhoneNumber).HasMaxLength(20);

        builder.Property(e => e.PrimaryAddress).IsRequired().HasMaxLength(300);
        builder.Property(e => e.PrimaryCity).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PrimaryState).IsRequired().HasMaxLength(50);
        builder.Property(e => e.PrimaryPostalCode).IsRequired().HasMaxLength(10);

        builder.Property(e => e.SecondaryAddress).HasMaxLength(300);
        builder.Property(e => e.SecondaryCity).HasMaxLength(100);
        builder.Property(e => e.SecondaryState).HasMaxLength(50);
        builder.Property(e => e.SecondaryPostalCode).HasMaxLength(10);

        builder.Property(e => e.WebsiteUrl).HasMaxLength(500);

        builder.HasIndex(e => e.CompanyCode).IsUnique();
        builder.HasIndex(e => e.PrimaryEmail).IsUnique();
    }
}
