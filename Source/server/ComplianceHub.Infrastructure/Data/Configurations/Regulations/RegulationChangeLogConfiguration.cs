using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class RegulationChangeLogConfiguration : IEntityTypeConfiguration<RegulationChangeLog>
{
    public void Configure(EntityTypeBuilder<RegulationChangeLog> builder)
    {
        builder.ToTable("RegulationChangeLogs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.GovernmentEntityName).IsRequired().HasMaxLength(300);
        builder.Property(e => e.AgencyName).IsRequired().HasMaxLength(300);
        builder.Property(e => e.RegulationCategoryName).IsRequired().HasMaxLength(300);
        builder.Property(e => e.RegulationTypeName).IsRequired().HasMaxLength(300);
        builder.Property(e => e.RegulationSubtypeName).HasMaxLength(300);
        builder.Property(e => e.SectionNumber).IsRequired().HasMaxLength(100);
        builder.Property(e => e.SectionTitle).IsRequired().HasColumnType("text");
        builder.Property(e => e.PreviousContentHash).HasMaxLength(64);
        builder.Property(e => e.NewContentHash).HasMaxLength(64);
        builder.Property(e => e.PreviousHtmlContent).HasColumnType("text");
        builder.Property(e => e.NewHtmlContent).HasColumnType("text");
        builder.Property(e => e.CreatedBy).HasMaxLength(200);

        builder.HasIndex(e => e.ChangedAt);
        builder.HasIndex(e => e.GovernmentEntityId);
        builder.HasIndex(e => e.RegulationId);

        builder.HasOne(e => e.Regulation)
            .WithMany()
            .HasForeignKey(e => e.RegulationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
