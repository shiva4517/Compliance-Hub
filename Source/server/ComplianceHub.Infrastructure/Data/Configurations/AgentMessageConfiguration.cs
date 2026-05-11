using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class AgentMessageConfiguration : IEntityTypeConfiguration<AgentMessage>
{
    public void Configure(EntityTypeBuilder<AgentMessage> builder)
    {
        builder.ToTable("AgentMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.ToolName)
            .HasMaxLength(100);

        builder.Property(x => x.ToolPayloadJson)
            .HasColumnType("text");

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
    }
}
