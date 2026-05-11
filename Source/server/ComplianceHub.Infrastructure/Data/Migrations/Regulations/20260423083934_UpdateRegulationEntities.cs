using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations.Regulations
{
    /// <inheritdoc />
    public partial class UpdateRegulationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UniqueIdentifier",
                table: "RegulationTypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniqueIdentifier",
                table: "RegulationSubtypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniqueIdentifier",
                table: "Regulations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniqueIdentifier",
                table: "RegulationCategories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniqueIdentifier",
                table: "GovernmentEntities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniqueIdentifier",
                table: "Agencies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UniqueIdentifier",
                table: "RegulationTypes");

            migrationBuilder.DropColumn(
                name: "UniqueIdentifier",
                table: "RegulationSubtypes");

            migrationBuilder.DropColumn(
                name: "UniqueIdentifier",
                table: "Regulations");

            migrationBuilder.DropColumn(
                name: "UniqueIdentifier",
                table: "RegulationCategories");

            migrationBuilder.DropColumn(
                name: "UniqueIdentifier",
                table: "GovernmentEntities");

            migrationBuilder.DropColumn(
                name: "UniqueIdentifier",
                table: "Agencies");
        }
    }
}
