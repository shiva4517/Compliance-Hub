using ComplianceHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ComplianceHubDbContext))]
    [Migration("20260511100000_AddSubscriptionScopeToRegulationDetails")]
    public partial class AddSubscriptionScopeToRegulationDetails : Migration
    {
        // Adds subscription-scope columns to RegulationDetails so the same table
        // can carry both the legacy regulation-level rows and the new
        // subscription-level rows authored from the Subscriptions module.
        //
        // - SubscriptionId: nullable FK to Subscriptions; identifies the owning subscription.
        // - SubscribingLevel: nullable string; matches Subscription.SubscribingLevel for context.
        // - MinValue / MaxValue: numeric Data Range bounds for Condition.
        //
        // The previous unique index on RegulationId is replaced with two filtered
        // unique indexes so legacy and subscription-scoped rows can coexist without
        // collision:
        //   * IX_RegulationDetails_RegulationId_Legacy: unique per RegulationId where
        //     SubscriptionId IS NULL (preserves legacy invariant — one detail per regulation).
        //   * IX_RegulationDetails_SubscriptionId:      unique per SubscriptionId where
        //     SubscriptionId IS NOT NULL.

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<System.Guid>(
                name: "SubscriptionId",
                table: "RegulationDetails",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscribingLevel",
                table: "RegulationDetails",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "RegulationDetails",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "RegulationDetails",
                type: "numeric(18,4)",
                nullable: true);

            // Drop the existing unconditional unique index on RegulationId.
            // (Some environments may have created it under the default name
            // "IX_RegulationDetails_RegulationId" — drop defensively.)
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_RegulationDetails_RegulationId') THEN
                        EXECUTE 'DROP INDEX "IX_RegulationDetails_RegulationId"';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RegulationDetails_RegulationId_Legacy",
                table: "RegulationDetails",
                column: "RegulationId",
                unique: true,
                filter: "\"SubscriptionId\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationDetails_SubscriptionId",
                table: "RegulationDetails",
                column: "SubscriptionId",
                unique: true,
                filter: "\"SubscriptionId\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegulationDetails_SubscriptionId",
                table: "RegulationDetails");

            migrationBuilder.DropIndex(
                name: "IX_RegulationDetails_RegulationId_Legacy",
                table: "RegulationDetails");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationDetails_RegulationId",
                table: "RegulationDetails",
                column: "RegulationId",
                unique: true);

            migrationBuilder.DropColumn(name: "MaxValue", table: "RegulationDetails");
            migrationBuilder.DropColumn(name: "MinValue", table: "RegulationDetails");
            migrationBuilder.DropColumn(name: "SubscribingLevel", table: "RegulationDetails");
            migrationBuilder.DropColumn(name: "SubscriptionId", table: "RegulationDetails");
        }
    }
}
