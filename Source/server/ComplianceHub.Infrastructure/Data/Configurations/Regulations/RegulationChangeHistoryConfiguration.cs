using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class RegulationChangeHistoryConfiguration : IEntityTypeConfiguration<RegulationChangeHistory>
{
    public void Configure(EntityTypeBuilder<RegulationChangeHistory> builder)
    {
        builder.ToTable("RegulationChangeHistory");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SectionNumber).IsRequired().HasMaxLength(100);
        builder.Property(e => e.HtmlContent).HasColumnType("text");
        builder.Property(e => e.ContentHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => new { e.RegulationId, e.Version });

        builder.HasOne(e => e.Regulation)
            .WithMany(r => r.ChangeHistory)
            .HasForeignKey(e => e.RegulationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
