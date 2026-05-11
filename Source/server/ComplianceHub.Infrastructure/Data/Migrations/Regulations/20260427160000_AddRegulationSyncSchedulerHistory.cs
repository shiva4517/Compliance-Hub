using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations.Regulations
{
    /// <inheritdoc />
    public partial class AddRegulationSyncSchedulerHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegulationSyncSchedulerHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GovernmentEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImportedRecordsCount = table.Column<int>(type: "integer", nullable: false),
                    ChangedRecordsCount = table.Column<int>(type: "integer", nullable: false),
                    DeactivatedRecordsCount = table.Column<int>(type: "integer", nullable: false),
                    OutboxEventsQueuedCount = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationSyncSchedulerHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulationSyncSchedulerHistory_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegulationSyncSchedulerHistory_GovernmentEntityId_StartedAt",
                table: "RegulationSyncSchedulerHistory",
                columns: new[] { "GovernmentEntityId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegulationSyncSchedulerHistory");
        }
    }
}
