using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations.Regulations;

public class GovernmentEntityConfiguration : IEntityTypeConfiguration<GovernmentEntity>
{
    public void Configure(EntityTypeBuilder<GovernmentEntity> builder)
    {
        builder.ToTable("GovernmentEntities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TitleNumber).IsRequired();
        builder.Property(e => e.TitleName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Source).HasMaxLength(500);
        builder.HasIndex(e => e.TitleNumber).IsUnique();
    }
}
