using ComplianceHub.Functions.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Functions.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options)
    : DbContext(options)
{
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<NotificationSentHistory> NotificationSentHistory => Set<NotificationSentHistory>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<NotificationHistory>(e =>
        {
            e.ToTable("NotificationHistory", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.Body).HasColumnType("text");
            e.Property(x => x.Subject).HasColumnType("text");
            e.Property(x => x.FailureReason).HasColumnType("text");
            e.Property(x => x.GovernmentEntityName).HasColumnType("text");
            e.Property(x => x.AgencyName).HasColumnType("text");
            e.Property(x => x.RegulationCategoryName).HasColumnType("text");
            e.Property(x => x.RegulationTypeName).HasColumnType("text");
            e.Property(x => x.RegulationSubtypeName).HasColumnType("text");
            e.Property(x => x.PreviousRegulationName).HasColumnType("text");
            e.Property(x => x.PresentRegulationName).HasColumnType("text");
        });

        mb.Entity<NotificationOutbox>(e =>
        {
            e.ToTable("NotificationOutbox", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.FailureReason).HasColumnType("text");
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.NotificationHistoryId);
        });

        mb.Entity<NotificationSentHistory>(e =>
        {
            e.ToTable("NotificationSentHistory", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.FailureReason).HasColumnType("text");
            e.HasIndex(x => x.NotificationHistoryId);
        });
    }
}
