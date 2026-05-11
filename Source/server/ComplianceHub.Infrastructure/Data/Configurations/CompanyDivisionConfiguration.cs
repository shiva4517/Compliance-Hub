using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class CompanyDivisionConfiguration : IEntityTypeConfiguration<CompanyDivision>
{
    public void Configure(EntityTypeBuilder<CompanyDivision> builder)
    {
        builder.ToTable("CompanyDivisions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);

        builder.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique();

        builder.HasOne(e => e.Company)
            .WithMany(c => c.Divisions)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Divisions)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
