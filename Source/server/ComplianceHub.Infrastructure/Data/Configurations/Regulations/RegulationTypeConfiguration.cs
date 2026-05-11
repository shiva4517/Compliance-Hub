using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class RegulationTypeConfiguration : IEntityTypeConfiguration<RegulationType>
{
    public void Configure(EntityTypeBuilder<RegulationType> builder)
    {
        builder.ToTable("RegulationTypes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PartNumber).IsRequired();
        builder.Property(e => e.PartName).IsRequired().HasMaxLength(500);
        builder.HasIndex(e => new { e.RegulationCategoryId, e.PartNumber }).IsUnique();

        builder.HasOne(e => e.GovernmentEntity)
            .WithMany()
            .HasForeignKey(e => e.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Agency)
            .WithMany()
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegulationCategory)
            .WithMany(c => c.RegulationTypes)
            .HasForeignKey(e => e.RegulationCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
