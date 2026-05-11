using ComplianceHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ComplianceHubDbContext))]
    [Migration("20260505000000_AddNotificationOutboxTrigger")]
    public partial class AddNotificationOutboxTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION fn_after_notification_history_insert()
                RETURNS TRIGGER
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    INSERT INTO "NotificationOutbox" (
                        "Id",
                        "NotificationHistoryId",
                        "Status",
                        "CreatedAt",
                        "ProcessedAt",
                        "FailureReason"
                    )
                    VALUES (
                        gen_random_uuid(),
                        NEW."Id",
                        'Pending',
                        NOW() AT TIME ZONE 'UTC',
                        NULL,
                        NULL
                    );

                    RETURN NEW;
                END;
                $$;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_trigger
                        WHERE tgname = 'trg_after_notification_history_insert'
                    ) THEN
                        CREATE TRIGGER trg_after_notification_history_insert
                            AFTER INSERT
                            ON "NotificationHistory"
                            FOR EACH ROW
                        EXECUTE FUNCTION fn_after_notification_history_insert();
                    END IF;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_after_notification_history_insert
                    ON "NotificationHistory";

                DROP FUNCTION IF EXISTS fn_after_notification_history_insert();
                """);
        }
    }
}
