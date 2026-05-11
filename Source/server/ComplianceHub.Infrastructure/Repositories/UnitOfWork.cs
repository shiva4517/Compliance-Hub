using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Infrastructure.Data;

namespace ComplianceHub.Infrastructure.Repositories;

public class UnitOfWork(ComplianceHubDbContext context) : IUnitOfWork
{
    private IRepository<SecurityUser>? _securityUsers;
    private IRepository<SecurityGroup>? _securityGroups;
    private IRepository<Customer>? _customers;
    private IRepository<ChangeNotice>? _changeNotices;
    private IRepository<Subscription>? _subscriptions;
    private IRepository<Grievance>? _grievances;
    private IRepository<GrievanceReply>? _grievanceReplies;
    private IRepository<NotificationHistory>? _notificationHistory;
    private IRepository<Company>? _companies;
    private IRepository<CompanyDivision>? _companyDivisions;
    private IRepository<Department>? _departments;
    private IRepository<District>? _districts;
    private IRepository<Employee>? _employees;
    private IRepository<EmployeeAssignedWork>? _employeeAssignedWorks;
    private IRepository<FrequencyType>? _frequencyTypes;
    private IRepository<DueDateType>? _dueDateTypes;
    private IRepository<RegulationDetail>? _regulationDetails;
    private IRepository<NotificationSentHistory>? _notificationSentHistories;
    private IRepository<AiProviderConnection>? _aiProviderConnections;
    private IRepository<AgentConversation>? _agentConversations;
    private IRepository<AgentMessage>? _agentMessages;

    public IRepository<SecurityUser> SecurityUsers =>
        _securityUsers ??= new GenericRepository<SecurityUser>(context);

    public IRepository<SecurityGroup> SecurityGroups =>
        _securityGroups ??= new GenericRepository<SecurityGroup>(context);

    public IRepository<Customer> Customers =>
        _customers ??= new GenericRepository<Customer>(context);

    public IRepository<ChangeNotice> ChangeNotices =>
        _changeNotices ??= new GenericRepository<ChangeNotice>(context);

    public IRepository<Subscription> Subscriptions =>
        _subscriptions ??= new GenericRepository<Subscription>(context);

    public IRepository<Grievance> Grievances =>
        _grievances ??= new GenericRepository<Grievance>(context);

    public IRepository<GrievanceReply> GrievanceReplies =>
        _grievanceReplies ??= new GenericRepository<GrievanceReply>(context);

    public IRepository<NotificationHistory> NotificationHistory =>
        _notificationHistory ??= new GenericRepository<NotificationHistory>(context);

    public IRepository<Company> Companies =>
        _companies ??= new GenericRepository<Company>(context);

    public IRepository<CompanyDivision> CompanyDivisions =>
        _companyDivisions ??= new GenericRepository<CompanyDivision>(context);

    public IRepository<Department> Departments =>
        _departments ??= new GenericRepository<Department>(context);

    public IRepository<District> Districts =>
        _districts ??= new GenericRepository<District>(context);

    public IRepository<Employee> Employees =>
        _employees ??= new GenericRepository<Employee>(context);

    public IRepository<EmployeeAssignedWork> EmployeeAssignedWorks =>
        _employeeAssignedWorks ??= new GenericRepository<EmployeeAssignedWork>(context);

    public IRepository<FrequencyType> FrequencyTypes =>
        _frequencyTypes ??= new GenericRepository<FrequencyType>(context);

    public IRepository<DueDateType> DueDateTypes =>
        _dueDateTypes ??= new GenericRepository<DueDateType>(context);

    public IRepository<RegulationDetail> RegulationDetails =>
        _regulationDetails ??= new GenericRepository<RegulationDetail>(context);

    public IRepository<NotificationSentHistory> NotificationSentHistories =>
        _notificationSentHistories ??= new GenericRepository<NotificationSentHistory>(context);

    public IRepository<AiProviderConnection> AiProviderConnections =>
        _aiProviderConnections ??= new GenericRepository<AiProviderConnection>(context);

    public IRepository<AgentConversation> AgentConversations =>
        _agentConversations ??= new GenericRepository<AgentConversation>(context);

    public IRepository<AgentMessage> AgentMessages =>
        _agentMessages ??= new GenericRepository<AgentMessage>(context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public void Dispose() => context.Dispose();
}
