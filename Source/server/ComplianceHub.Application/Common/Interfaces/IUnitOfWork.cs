using ComplianceHub.Domain.Entities;

namespace ComplianceHub.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<SecurityUser> SecurityUsers { get; }
    IRepository<SecurityGroup> SecurityGroups { get; }
    IRepository<Customer> Customers { get; }
    IRepository<ChangeNotice> ChangeNotices { get; }
    IRepository<Subscription> Subscriptions { get; }
    IRepository<Grievance> Grievances { get; }
    IRepository<GrievanceReply> GrievanceReplies { get; }
    IRepository<NotificationHistory> NotificationHistory { get; }
    IRepository<Company> Companies { get; }
    IRepository<CompanyDivision> CompanyDivisions { get; }
    IRepository<Department> Departments { get; }
    IRepository<District> Districts { get; }
    IRepository<Employee> Employees { get; }
    IRepository<EmployeeAssignedWork> EmployeeAssignedWorks { get; }
    IRepository<FrequencyType> FrequencyTypes { get; }
    IRepository<DueDateType> DueDateTypes { get; }
    IRepository<RegulationDetail> RegulationDetails { get; }
    IRepository<NotificationSentHistory> NotificationSentHistories { get; }
    IRepository<AiProviderConnection> AiProviderConnections { get; }
    IRepository<AgentConversation> AgentConversations { get; }
    IRepository<AgentMessage> AgentMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
