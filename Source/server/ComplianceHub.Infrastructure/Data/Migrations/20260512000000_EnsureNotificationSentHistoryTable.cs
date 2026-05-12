using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnsureNotificationSentHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent recreate: the original AddNotificationSentHistory migration
            // is already recorded in __EFMigrationsHistory on some environments
            // even though the table is missing in Postgres. Plain CREATE TABLE
            // would fail there; this raw SQL is safe to run regardless of state.
            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_NotificationSentHistory_NotificationHistoryId";
                DROP TABLE IF EXISTS "NotificationSentHistory";
                """);
        }
    }
}
