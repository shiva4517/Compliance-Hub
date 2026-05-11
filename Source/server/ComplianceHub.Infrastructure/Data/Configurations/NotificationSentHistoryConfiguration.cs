using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class NotificationSentHistoryConfiguration : IEntityTypeConfiguration<NotificationSentHistory>
{
    public void Configure(EntityTypeBuilder<NotificationSentHistory> builder)
    {
        builder.ToTable("NotificationSentHistory");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.FailureReason).HasColumnType("text");
        builder.Property(e => e.AttemptedAt).IsRequired();

        builder.HasIndex(e => e.NotificationHistoryId);
    }
}
