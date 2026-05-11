using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(e => new { e.CompanyId, e.EmployeeCode }).IsUnique();

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PrimaryEmail).IsRequired().HasMaxLength(200);
        builder.HasIndex(e => new { e.CompanyId, e.PrimaryEmail }).IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.Property(e => e.SecondaryEmail).HasMaxLength(200);
        builder.Property(e => e.PhoneNumber).HasMaxLength(20);
        builder.Property(e => e.MobileNumber).HasMaxLength(20);

        builder.Property(e => e.PrimaryAddress).IsRequired().HasMaxLength(300);
        builder.Property(e => e.PrimaryCity).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PrimaryState).IsRequired().HasMaxLength(50);
        builder.Property(e => e.PrimaryPostalCode).IsRequired().HasMaxLength(10);

        builder.Property(e => e.SecondaryAddress).HasMaxLength(300);
        builder.Property(e => e.SecondaryCity).HasMaxLength(100);
        builder.Property(e => e.SecondaryState).HasMaxLength(50);
        builder.Property(e => e.SecondaryPostalCode).HasMaxLength(10);

        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CompanyDivision)
            .WithMany()
            .HasForeignKey(e => e.CompanyDivisionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.District)
            .WithMany()
            .HasForeignKey(e => e.CompanyDistrictId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
