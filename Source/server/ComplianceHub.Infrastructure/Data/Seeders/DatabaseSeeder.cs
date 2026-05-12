using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceHub.Infrastructure.Data.Seeders;

public class DatabaseSeeder(ComplianceHubDbContext context, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync()
    {
        try
        {
            await context.Database.MigrateAsync();
            await EnsureNotificationSentHistoryTableAsync();
            await SeedGroupsAsync();
            var company = await SeedDefaultCompanyAsync();
            await SeedUsersAsync(company);
            await SeedCustomersAsync(company);
            await SeedFrequencyTypes();
            await SeedDueDateTypes();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    // Some environments have the AddNotificationSentHistory migration recorded
    // in __EFMigrationsHistory but the table missing from Postgres. Guarantee
    // it exists at startup with a single idempotent CREATE TABLE so the
    // notification pipeline can always insert attempt rows.
    private async Task EnsureNotificationSentHistoryTableAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "NotificationSentHistory" (
                "Id"                    uuid                        NOT NULL,
                "NotificationHistoryId" uuid                        NOT NULL,
                "AttemptNumber"         integer                     NOT NULL,
                "IsSuccess"             boolean                     NOT NULL,
                "Status"                character varying(20)       NOT NULL,
                "FailureReason"         text                        NULL,
                "AttemptedAt"           timestamp with time zone    NOT NULL,
                CONSTRAINT "PK_NotificationSentHistory" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_NotificationSentHistory_NotificationHistoryId"
                ON "NotificationSentHistory" ("NotificationHistoryId");
            """;

        await context.Database.ExecuteSqlRawAsync(sql);
        logger.LogInformation("Ensured NotificationSentHistory table exists.");
    }

    private async Task SeedDueDateTypes()
    {
        if (await context.DueDateTypes.IgnoreQueryFilters().AnyAsync()) return;

        var dueDateTypes = new[]
        {
            new DueDateType { Name = "Fixed Date", Description ="A specific calendar date (e.g., Dec 31st)" },
            new DueDateType { Name = "Day of Month", Description = "A specific day number (e.g., the 5th of every month)" },
            new DueDateType { Name = "Relative to Start", Description = "X days after the start/effective date" },
            new DueDateType { Name = "End of Month", Description = "Always the last day of the current month" },
            new DueDateType { Name = "Day of Week", Description = "A specific day (e.g., every Friday)" },
            new DueDateType { Name = "Immediate", Description = "Due upon receipt or generation" },
            new DueDateType { Name = "Milestone Based", Description = "Triggered by a specific project stage or event" }
        };

        await context.DueDateTypes.AddRangeAsync(dueDateTypes);
        await context.SaveChangesAsync();
        logger.LogInformation("DueDateTypes seeded.");
    }

    private async Task SeedFrequencyTypes()
    {
        if (await context.FrequencyTypes.IgnoreQueryFilters().AnyAsync()) return;

        var frequencyTypes = new[]
        {
            new FrequencyType { Name = "Daily", Description ="Every single day." },
            new FrequencyType { Name = "Weekly", Description = "Once every 7 days." },
            new FrequencyType { Name = "Bi-Weekly", Description = "Every two weeks (common for payroll)" },
            new FrequencyType { Name = "Monthly", Description = "Once a month on a specific date" },
            new FrequencyType { Name = "Quarterly", Description = "Every 3 months (4 times a year)" },
            new FrequencyType { Name = "Semi-Annually", Description = "Every 6 months" },
            new FrequencyType { Name = "Annually", Description = "Once a year" },
            new FrequencyType { Name = "One-Time", Description = "No recurrence; a single instance" }
        };

        await context.FrequencyTypes.AddRangeAsync(frequencyTypes);
        await context.SaveChangesAsync();
        logger.LogInformation("FrequencyTypes seeded.");
    }

    private async Task SeedGroupsAsync()
    {
        if (await context.SecurityGroups.IgnoreQueryFilters().AnyAsync()) return;

        var groups = new[]
        {
            new SecurityGroup { GroupName = "Super Administrators", Description = "Full system access", Role = UserRole.SuperAdmin },
            new SecurityGroup { GroupName = "Administrators", Description = "Administrative access", Role = UserRole.Admin },
            new SecurityGroup { GroupName = "Customers", Description = "Customer portal access", Role = UserRole.Customer },
            new SecurityGroup { GroupName = "Employees", Description = "Employee access", Role = UserRole.Employee }
        };

        await context.SecurityGroups.AddRangeAsync(groups);
        await context.SaveChangesAsync();
        logger.LogInformation("Security groups seeded.");
    }

    private async Task<Company> SeedDefaultCompanyAsync()
    {
        var existing = await context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync();
        if (existing != null) return existing;

        var company = new Company
        {
            CompanyCode = "COMP-0001",
            CompanyName = "Compliance Hub Default Company",
            PrimaryEmail = "admin@compliancehub.com",
            PrimaryAddress = "123 Main Street",
            PrimaryCity = "Austin",
            PrimaryState = "TX",
            PrimaryPostalCode = "78701",
            IsActive = true,
            CreatedBy = "system"
        };

        await context.Companies.AddAsync(company);
        await context.SaveChangesAsync();
        logger.LogInformation("Default company seeded.");
        return company;
    }

    private async Task SeedUsersAsync(Company company)
    {
        // Fix existing Admin users whose UserId was not set
        var adminUsersWithNullId = await context.SecurityUsers.IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.Admin && u.UserId == null)
            .ToListAsync();
        if (adminUsersWithNullId.Count > 0)
        {
            foreach (var u in adminUsersWithNullId)
                u.UserId = company.Id;
            await context.SaveChangesAsync();
            logger.LogInformation("Fixed {Count} Admin user(s) with missing UserId.", adminUsersWithNullId.Count);
        }

        if (await context.SecurityUsers.IgnoreQueryFilters().AnyAsync()) return;

        var superAdminGroup = await context.SecurityGroups.IgnoreQueryFilters()
            .FirstAsync(g => g.Role == UserRole.SuperAdmin);
        var adminGroup = await context.SecurityGroups.IgnoreQueryFilters()
            .FirstAsync(g => g.Role == UserRole.Admin);

        var users = new[]
        {
            new SecurityUser
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@compliancehub.com",
                PasswordHash = ComputeSha512("Admin@123"),
                Role = UserRole.SuperAdmin,
                SecurityGroupId = superAdminGroup.Id,
                Title = "System Administrator",
                CreatedBy = "system"
            },
            new SecurityUser
            {
                FirstName = "John",
                LastName = "Admin",
                Email = "john.admin@compliancehub.com",
                PasswordHash = ComputeSha512("Admin@123"),
                Role = UserRole.Admin,
                SecurityGroupId = adminGroup.Id,
                Title = "Administrator",
                UserId = company.Id,
                CreatedBy = "system"
            }
        };

        await context.SecurityUsers.AddRangeAsync(users);
        await context.SaveChangesAsync();
        logger.LogInformation("Default users seeded.");
    }

    private async Task SeedCustomersAsync(Company company)
    {
        if (await context.Customers.IgnoreQueryFilters().AnyAsync()) return;

        var customerGroup = await context.SecurityGroups.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Role == UserRole.Customer);

        var seedData = new[]
        {
            ("Alcoa Rockdale Smelter", "John", "Doe", "rockdale.env@alcoa.com"),
            ("Celanese Clear Lake Plant", "Jane", "Smith", "eehs@celanese.com"),
            ("Corpus Christi Desalination Plant", "Robert", "Jones", "waterops@cctexas.com"),
            ("Dow Chemical Freeport Complex", "Emily", "Williams", "txenviro@dow.com"),
            ("Luminant Martin Lake Power Plant", "Michael", "Brown", "envpermit@luminant.com"),
            ("Phillips 66 Sweeny Refinery", "Sarah", "Taylor", "txenv@phillips66.com"),
            ("San Antonio Water System WWTP", "David", "Martinez", "compliance@saws.org"),
            ("Tyson Foods Seguin Processing", "Laura", "Anderson", "env.seguin@tyson.com"),
            ("Valero Houston Refinery", "James", "Thomas", "envcomp@valero.com"),
            ("Waste Management Atascocita Landfill", "Karen", "Jackson", "txpermits@wm.com"),
        };

        var customers = new List<Customer>();
        var secUsers = new List<SecurityUser>();

        for (var i = 0; i < seedData.Length; i++)
        {
            var (name, firstName, lastName, email) = seedData[i];
            var customer = new Customer
            {
                CompanyId = company.Id,
                CustomerCode = $"CUST-{i + 1:D4}",
                CustomerName = name,
                PrimaryContactFirstName = firstName,
                PrimaryContactLastName = lastName,
                PrimaryEmail = email,
                IsActive = true,
                CreatedBy = "system"
            };
            customers.Add(customer);

            if (customerGroup is not null)
            {
                secUsers.Add(new SecurityUser
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PasswordHash = ComputeSha512("Customer@123"),
                    Role = UserRole.Customer,
                    SecurityGroupId = customerGroup.Id,
                    UserId = customer.Id,
                    IsActive = true,
                    IsForcePasswordChange = true,
                    CreatedBy = "system"
                });
            }
        }

        await context.Customers.AddRangeAsync(customers);
        if (secUsers.Count > 0)
            await context.SecurityUsers.AddRangeAsync(secUsers);
        await context.SaveChangesAsync();
        logger.LogInformation("Sample customers seeded.");
    }

    private static string ComputeSha512(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
