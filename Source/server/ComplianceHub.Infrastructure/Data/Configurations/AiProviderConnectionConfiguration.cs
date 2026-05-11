using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class AiProviderConnectionConfiguration : IEntityTypeConfiguration<AiProviderConnection>
{
    public void Configure(EntityTypeBuilder<AiProviderConnection> builder)
    {
        builder.ToTable("AiProviderConnections");

        builder.Property(x => x.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EncryptedApiKey)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Endpoint)
            .HasMaxLength(500);

        builder.Property(x => x.DeploymentName)
            .HasMaxLength(200);

        builder.Property(x => x.Model)
            .HasMaxLength(200);

        builder.Property(x => x.ApiVersion)
            .HasMaxLength(50);

        builder.HasIndex(x => x.SecurityUserId)
            .IsUnique();

        builder.HasOne(x => x.SecurityUser)
            .WithMany()
            .HasForeignKey(x => x.SecurityUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
