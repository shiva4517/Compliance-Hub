using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations.Regulations
{
    public partial class RepairRegulationSyncSchedulerHistoryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "RegulationSyncSchedulerHistory" (
                    "Id" uuid NOT NULL,
                    "GovernmentEntityId" uuid NOT NULL,
                    "OperationType" character varying(50) NOT NULL,
                    "Status" character varying(50) NOT NULL,
                    "ImportedRecordsCount" integer NOT NULL,
                    "ChangedRecordsCount" integer NOT NULL,
                    "DeactivatedRecordsCount" integer NOT NULL,
                    "OutboxEventsQueuedCount" integer NOT NULL,
                    "Details" character varying(2000) NULL,
                    "StartedAt" timestamp with time zone NOT NULL,
                    "CompletedAt" timestamp with time zone NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NULL,
                    "CreatedBy" text NULL,
                    "UpdatedBy" text NULL,
                    "IsDeleted" boolean NOT NULL,
                    CONSTRAINT "PK_RegulationSyncSchedulerHistory" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_RegulationSyncSchedulerHistory_GovernmentEntities_GovernmentEntityId"
                        FOREIGN KEY ("GovernmentEntityId") REFERENCES "GovernmentEntities" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_RegulationSyncSchedulerHistory_GovernmentEntityId_StartedAt"
                ON "RegulationSyncSchedulerHistory" ("GovernmentEntityId", "StartedAt");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegulationSyncSchedulerHistory");
        }
    }
}
