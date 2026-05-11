using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class RegulationSubtypeConfiguration : IEntityTypeConfiguration<RegulationSubtype>
{
    public void Configure(EntityTypeBuilder<RegulationSubtype> builder)
    {
        builder.ToTable("RegulationSubtypes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SubpartIdentifier).IsRequired().HasMaxLength(50);
        builder.Property(e => e.SubpartName).IsRequired().HasMaxLength(500);
        builder.HasIndex(e => new { e.RegulationTypeId, e.SubpartIdentifier }).IsUnique();

        builder.HasOne(e => e.RegulationType)
            .WithMany(t => t.RegulationSubtypes)
            .HasForeignKey(e => e.RegulationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
