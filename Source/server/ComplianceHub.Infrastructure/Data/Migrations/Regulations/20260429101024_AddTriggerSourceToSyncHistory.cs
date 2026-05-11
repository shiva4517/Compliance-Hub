using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations.Regulations
{
    /// <inheritdoc />
    public partial class AddTriggerSourceToSyncHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TriggerSource",
                table: "RegulationSyncSchedulerHistory",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "RegulationSyncSchedulerHistory");
        }
    }
}
