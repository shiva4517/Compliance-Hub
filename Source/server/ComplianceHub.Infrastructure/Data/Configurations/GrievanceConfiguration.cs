using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class GrievanceConfiguration : IEntityTypeConfiguration<Grievance>
{
    public void Configure(EntityTypeBuilder<Grievance> builder)
    {
        builder.ToTable("Grievances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.CreatedByUserType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.RecipientUserType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Priority).IsRequired().HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.LastMessageAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.SubscriptionId);
        builder.HasIndex(x => x.CreatedBySecurityUserId);
        builder.HasIndex(x => x.RecipientSecurityUserId);
        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasIndex(x => x.LastMessageAt);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Subscription)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CreatedBySecurityUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBySecurityUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RecipientSecurityUser)
            .WithMany()
            .HasForeignKey(x => x.RecipientSecurityUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
