using ComplianceHub.Domain.Entities.Regulations;
using Microsoft.EntityFrameworkCore.Storage;

namespace ComplianceHub.Application.Common.Interfaces;

public interface IRegulationsUnitOfWork : IDisposable
{
    IRepository<GovernmentEntity> GovernmentEntities { get; }
    IRepository<Agency> Agencies { get; }
    IRepository<RegulationCategory> RegulationCategories { get; }
    IRepository<RegulationType> RegulationTypes { get; }
    IRepository<RegulationSubtype> RegulationSubtypes { get; }
    IRepository<Regulation> Regulations { get; }
    IRepository<RegulationChangeHistory> RegulationChangeHistory { get; }
    IRepository<RegulationSyncSchedulerHistory> RegulationSyncSchedulerHistories { get; }
    IRepository<OutboxEvent> OutboxEvents { get; }
    IRepository<RegulationChangeLog> RegulationChangeLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}
