using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceHub.Infrastructure.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SubscribingLevel).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.SubscribedNodeName).HasMaxLength(500);

        builder.HasOne(e => e.Customer)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
