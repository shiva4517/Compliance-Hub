using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("NotificationOutbox");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.FailureReason).HasColumnType("text");
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.NotificationHistoryId);
    }
}
