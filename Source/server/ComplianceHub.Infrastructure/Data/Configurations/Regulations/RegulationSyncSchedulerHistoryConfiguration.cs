using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class RegulationSyncSchedulerHistoryConfiguration : IEntityTypeConfiguration<RegulationSyncSchedulerHistory>
{
    public void Configure(EntityTypeBuilder<RegulationSyncSchedulerHistory> builder)
    {
        builder.ToTable("RegulationSyncSchedulerHistory");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.OperationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TriggerSource)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Manual");

        builder.Property(e => e.Details)
            .HasMaxLength(2000);

        builder.HasIndex(e => new { e.GovernmentEntityId, e.StartedAt });

        builder.HasOne(e => e.GovernmentEntity)
            .WithMany()
            .HasForeignKey(e => e.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
