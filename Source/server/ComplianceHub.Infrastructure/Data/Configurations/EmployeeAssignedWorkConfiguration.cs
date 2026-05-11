using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class EmployeeAssignedWorkConfiguration : IEntityTypeConfiguration<EmployeeAssignedWork>
{
    public void Configure(EntityTypeBuilder<EmployeeAssignedWork> builder)
    {
        builder.ToTable("EmployeeAssignedWork");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AssignedBy).IsRequired().HasMaxLength(200);
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => new { e.EmployeeId, e.SubscriptionId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Subscription)
            .WithMany()
            .HasForeignKey(e => e.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
