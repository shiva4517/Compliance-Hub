using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class NotificationHistoryConfiguration : IEntityTypeConfiguration<NotificationHistory>
{
    public void Configure(EntityTypeBuilder<NotificationHistory> builder)
    {
        builder.ToTable("NotificationHistory");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
        builder.Property(e => e.Subject).HasColumnType("text");
        builder.Property(e => e.Body).HasColumnType("text");
        builder.Property(e => e.FailureReason).HasColumnType("text");
        builder.Property(e => e.GovernmentEntityName).HasColumnType("text");
        builder.Property(e => e.AgencyName).HasColumnType("text");
        builder.Property(e => e.RegulationCategoryName).HasColumnType("text");
        builder.Property(e => e.RegulationTypeName).HasColumnType("text");
        builder.Property(e => e.RegulationSubtypeName).HasColumnType("text");
        builder.Property(e => e.PreviousRegulationName).HasColumnType("text");
        builder.Property(e => e.PresentRegulationName).HasColumnType("text");

        builder.Property(e => e.IsSeen).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.SeenAt).IsRequired(false);

        builder.HasIndex(e => new { e.CustomerId, e.Status });
        builder.HasIndex(e => new { e.CompanyId, e.Status });
        builder.HasIndex(e => e.CompanyId);
        builder.HasIndex(e => new { e.CompanyId, e.IsSeen });

        builder.HasOne(e => e.Subscription)
            .WithMany(s => s.NotificationHistories)
            .HasForeignKey(e => e.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Customer)
            .WithMany(c => c.NotificationHistories)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
