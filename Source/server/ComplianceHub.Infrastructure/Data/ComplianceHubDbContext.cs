using ComplianceHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Infrastructure.Data;

public class ComplianceHubDbContext(DbContextOptions<ComplianceHubDbContext> options) : DbContext(options)
{
    public DbSet<SecurityUser> SecurityUsers => Set<SecurityUser>();
    public DbSet<SecurityGroup> SecurityGroups => Set<SecurityGroup>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ChangeNotice> ChangeNotices => Set<ChangeNotice>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Grievance> Grievances => Set<Grievance>();
    public DbSet<GrievanceReply> GrievanceReplies => Set<GrievanceReply>();
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyDivision> CompanyDivisions => Set<CompanyDivision>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeAssignedWork> EmployeeAssignedWorks => Set<EmployeeAssignedWork>();
    public DbSet<FrequencyType> FrequencyTypes => Set<FrequencyType>();
    public DbSet<DueDateType> DueDateTypes => Set<DueDateType>();
    public DbSet<RegulationDetail> RegulationDetails => Set<RegulationDetail>();
    public DbSet<NotificationSentHistory> NotificationSentHistories => Set<NotificationSentHistory>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<AiProviderConnection> AiProviderConnections => Set<AiProviderConnection>();
    public DbSet<AgentConversation> AgentConversations => Set<AgentConversation>();
    public DbSet<AgentMessage> AgentMessages => Set<AgentMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ComplianceHubDbContext).Assembly,
            t => t.Namespace == "ComplianceHub.Infrastructure.Data.Configurations");

        // Global soft-delete filter
        modelBuilder.Entity<SecurityUser>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SecurityGroup>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Grievance>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GrievanceReply>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Company>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CompanyDivision>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<District>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EmployeeAssignedWork>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FrequencyType>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DueDateType>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RegulationDetail>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AiProviderConnection>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AgentConversation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AgentMessage>().HasQueryFilter(e => !e.IsDeleted);
    }
}
