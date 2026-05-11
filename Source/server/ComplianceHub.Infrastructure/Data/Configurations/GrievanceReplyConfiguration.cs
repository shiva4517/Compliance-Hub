using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class GrievanceReplyConfiguration : IEntityTypeConfiguration<GrievanceReply>
{
    public void Configure(EntityTypeBuilder<GrievanceReply> builder)
    {
        builder.ToTable("GrievanceReplies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.SenderUserType).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.GrievanceId);
        builder.HasIndex(x => new { x.GrievanceId, x.CreatedAt });
        builder.HasIndex(x => x.SenderSecurityUserId);

        builder.HasOne(x => x.Grievance)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.GrievanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SenderSecurityUser)
            .WithMany()
            .HasForeignKey(x => x.SenderSecurityUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
