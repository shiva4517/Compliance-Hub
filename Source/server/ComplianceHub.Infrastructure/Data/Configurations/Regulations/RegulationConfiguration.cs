using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class RegulationConfiguration : IEntityTypeConfiguration<Regulation>
{
    public void Configure(EntityTypeBuilder<Regulation> builder)
    {
        builder.ToTable("Regulations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SectionNumber).IsRequired().HasMaxLength(100);
        builder.Property(e => e.SectionName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.HtmlContent).HasColumnType("text");
        builder.Property(e => e.ContentHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(e => new { e.RegulationTypeId, e.SectionNumber })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");

        builder.HasOne(e => e.GovernmentEntity)
            .WithMany()
            .HasForeignKey(e => e.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Agency)
            .WithMany()
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegulationCategory)
            .WithMany()
            .HasForeignKey(e => e.RegulationCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegulationType)
            .WithMany(t => t.Regulations)
            .HasForeignKey(e => e.RegulationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegulationSubtype)
            .WithMany(s => s.Regulations)
            .HasForeignKey(e => e.RegulationSubtypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
