using ComplianceHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ComplianceHubDbContext))]
    [Migration("20260511000000_RebrandSeedDataToComplianceHub")]
    public partial class RebrandSeedDataToComplianceHub : Migration
    {
        // Data-only migration: rebrands seeded master rows (default Company + seeded
        // admin users) from "ARM" to "Compliance Hub". All updates are guarded by
        // the legacy value so rows already customized by tenants are left untouched.
        // No schema changes — table/column/constraint names are already neutral in
        // the current model. (Schema-level "ARM" identifiers were already removed
        // by migration 20260420124216_ReplaceARMCustomerWithCustomer, which dropped
        // the legacy "ARMCustomers" table.)

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default Company seed row
            migrationBuilder.Sql("""
                UPDATE "Companies"
                SET "CompanyName" = 'Compliance Hub Default Company'
                WHERE "CompanyName" = 'ARM Default Company';

                UPDATE "Companies"
                SET "PrimaryEmail" = 'admin@compliancehub.com'
                WHERE "PrimaryEmail" = 'admin@armdefault.com';
                """);

            // Seeded admin SecurityUser rows — only update if still on the legacy
            // default email (preserves any tenant-modified credentials).
            migrationBuilder.Sql("""
                UPDATE "SecurityUsers"
                SET "Email" = 'admin@compliancehub.com'
                WHERE "Email" = 'admin@arm.com';

                UPDATE "SecurityUsers"
                SET "Email" = 'john.admin@compliancehub.com'
                WHERE "Email" = 'john.admin@arm.com';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Symmetric rollback — restores legacy seed values where they still
            // match the rebranded defaults.
            migrationBuilder.Sql("""
                UPDATE "SecurityUsers"
                SET "Email" = 'john.admin@arm.com'
                WHERE "Email" = 'john.admin@compliancehub.com';

                UPDATE "SecurityUsers"
                SET "Email" = 'admin@arm.com'
                WHERE "Email" = 'admin@compliancehub.com';

                UPDATE "Companies"
                SET "PrimaryEmail" = 'admin@armdefault.com'
                WHERE "PrimaryEmail" = 'admin@compliancehub.com';

                UPDATE "Companies"
                SET "CompanyName" = 'ARM Default Company'
                WHERE "CompanyName" = 'Compliance Hub Default Company';
                """);
        }
    }
}
