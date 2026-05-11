using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Infrastructure.Data;

public class RegulationsDbContext(DbContextOptions<RegulationsDbContext> options) : DbContext(options)
{
    public DbSet<GovernmentEntity> GovernmentEntities => Set<GovernmentEntity>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<RegulationCategory> RegulationCategories => Set<RegulationCategory>();
    public DbSet<RegulationType> RegulationTypes => Set<RegulationType>();
    public DbSet<RegulationSubtype> RegulationSubtypes => Set<RegulationSubtype>();
    public DbSet<Regulation> Regulations => Set<Regulation>();
    public DbSet<RegulationChangeHistory> RegulationChangeHistory => Set<RegulationChangeHistory>();
    public DbSet<RegulationSyncSchedulerHistory> RegulationSyncSchedulerHistories => Set<RegulationSyncSchedulerHistory>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<RegulationChangeLog> RegulationChangeLogs => Set<RegulationChangeLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RegulationsDbContext).Assembly,
            t => t.Namespace == "ComplianceHub.Infrastructure.Data.Configurations.Regulations");

        modelBuilder.Entity<GovernmentEntity>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Agency>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RegulationCategory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RegulationType>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RegulationSubtype>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Regulation>().HasQueryFilter(e => !e.IsDeleted);
    }
}
