using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CustomerCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.PrimaryContactFirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PrimaryContactLastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PrimaryEmail).IsRequired().HasMaxLength(200);
        builder.Property(e => e.SecondaryEmail).HasMaxLength(200);
        builder.Property(e => e.PhoneNumber).HasMaxLength(20);
        builder.Property(e => e.MobileNumber).HasMaxLength(20);
        builder.Property(e => e.PrimaryAddress).HasMaxLength(300);
        builder.Property(e => e.PrimaryCity).HasMaxLength(100);
        builder.Property(e => e.PrimaryState).HasMaxLength(50);
        builder.Property(e => e.PrimaryPostalCode).HasMaxLength(10);
        builder.Property(e => e.SecondaryAddress).HasMaxLength(300);
        builder.Property(e => e.SecondaryCity).HasMaxLength(100);
        builder.Property(e => e.SecondaryState).HasMaxLength(50);
        builder.Property(e => e.SecondaryPostalCode).HasMaxLength(10);

        builder.HasIndex(e => new { e.CompanyId, e.PrimaryEmail })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => new { e.CompanyId, e.CustomerCode }).IsUnique();

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
