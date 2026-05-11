using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.ToTable("Agencies");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ChapterNumber).IsRequired().HasMaxLength(50);
        builder.Property(e => e.AgencyName).IsRequired().HasMaxLength(500);
        builder.HasIndex(e => new { e.GovernmentEntityId, e.ChapterNumber }).IsUnique();

        builder.HasOne(e => e.GovernmentEntity)
            .WithMany(g => g.Agencies)
            .HasForeignKey(e => e.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
