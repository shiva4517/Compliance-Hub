using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities.Regulations;
using ComplianceHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ComplianceHub.Infrastructure.Repositories;

public class RegulationsUnitOfWork(RegulationsDbContext context) : IRegulationsUnitOfWork
{
    private IRepository<GovernmentEntity>? _governmentEntities;
    private IRepository<Agency>? _agencies;
    private IRepository<RegulationCategory>? _regulationCategories;
    private IRepository<RegulationType>? _regulationTypes;
    private IRepository<RegulationSubtype>? _regulationSubtypes;
    private IRepository<Regulation>? _regulations;
    private IRepository<RegulationChangeHistory>? _regulationChangeHistory;
    private IRepository<RegulationSyncSchedulerHistory>? _regulationSyncSchedulerHistories;
    private IRepository<OutboxEvent>? _outboxEvents;
    private IRepository<RegulationChangeLog>? _regulationChangeLogs;

    public IRepository<GovernmentEntity> GovernmentEntities =>
        _governmentEntities ??= new GenericRepository<GovernmentEntity>(context);

    public IRepository<Agency> Agencies =>
        _agencies ??= new GenericRepository<Agency>(context);

    public IRepository<RegulationCategory> RegulationCategories =>
        _regulationCategories ??= new GenericRepository<RegulationCategory>(context);

    public IRepository<RegulationType> RegulationTypes =>
        _regulationTypes ??= new GenericRepository<RegulationType>(context);

    public IRepository<RegulationSubtype> RegulationSubtypes =>
        _regulationSubtypes ??= new GenericRepository<RegulationSubtype>(context);

    public IRepository<Regulation> Regulations =>
        _regulations ??= new GenericRepository<Regulation>(context);

    public IRepository<RegulationChangeHistory> RegulationChangeHistory =>
        _regulationChangeHistory ??= new GenericRepository<RegulationChangeHistory>(context);

    public IRepository<RegulationSyncSchedulerHistory> RegulationSyncSchedulerHistories =>
        _regulationSyncSchedulerHistories ??= new GenericRepository<RegulationSyncSchedulerHistory>(context);

    public IRepository<OutboxEvent> OutboxEvents =>
        _outboxEvents ??= new GenericRepository<OutboxEvent>(context);

    public IRepository<RegulationChangeLog> RegulationChangeLogs =>
        _regulationChangeLogs ??= new GenericRepository<RegulationChangeLog>(context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        await context.Database.BeginTransactionAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async retryCt =>
        {
            await using var tx = await context.Database.BeginTransactionAsync(retryCt);
            await operation(retryCt);
            await tx.CommitAsync(retryCt);
        }, ct);
    }

    public void Dispose() => context.Dispose();
}
