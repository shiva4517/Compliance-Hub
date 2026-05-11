using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class RegulationDetailConfiguration : IEntityTypeConfiguration<RegulationDetail>
{
    public void Configure(EntityTypeBuilder<RegulationDetail> builder)
    {
        builder.ToTable("RegulationDetails");
        builder.HasKey(e => e.Id);

        // Legacy (regulation-only) rows are unique per RegulationId AND have no SubscriptionId.
        // New subscription-scoped rows are unique per SubscriptionId.
        // Two filtered unique indexes capture both invariants without conflicting.
        builder.HasIndex(e => e.RegulationId)
            .HasDatabaseName("IX_RegulationDetails_RegulationId_Legacy")
            .IsUnique()
            .HasFilter("\"SubscriptionId\" IS NULL AND \"IsDeleted\" = false");

        builder.HasIndex(e => e.SubscriptionId)
            .HasDatabaseName("IX_RegulationDetails_SubscriptionId")
            .IsUnique()
            .HasFilter("\"SubscriptionId\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Condition).HasMaxLength(2000);
        builder.Property(e => e.SuggestedTask).HasMaxLength(2000);

        builder.Property(e => e.SubscribingLevel).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.MinValue).HasColumnType("numeric(18,4)");
        builder.Property(e => e.MaxValue).HasColumnType("numeric(18,4)");

        builder.HasOne(e => e.FrequencyType)
            .WithMany()
            .HasForeignKey(e => e.FrequencyTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.DueDateType)
            .WithMany()
            .HasForeignKey(e => e.DueDateTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
