using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class RegulationCategoryConfiguration : IEntityTypeConfiguration<RegulationCategory>
{
    public void Configure(EntityTypeBuilder<RegulationCategory> builder)
    {
        builder.ToTable("RegulationCategories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SubchapterIdentifier).IsRequired().HasMaxLength(50);
        builder.Property(e => e.SubchapterName).IsRequired().HasMaxLength(500);
        builder.HasIndex(e => new { e.AgencyId, e.SubchapterIdentifier }).IsUnique();

        builder.HasOne(e => e.GovernmentEntity)
            .WithMany()
            .HasForeignKey(e => e.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Agency)
            .WithMany(a => a.RegulationCategories)
            .HasForeignKey(e => e.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
