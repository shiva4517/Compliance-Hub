using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations.Regulations
{
    /// <inheritdoc />
    public partial class RenameUniqueIdentifierToIdentifierAndBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UniqueIdentifier",
                table: "RegulationTypes",
                newName: "Identifier");

            migrationBuilder.RenameColumn(
                name: "UniqueIdentifier",
                table: "RegulationSubtypes",
                newName: "Identifier");

            migrationBuilder.RenameColumn(
                name: "UniqueIdentifier",
                table: "Regulations",
                newName: "Identifier");

            migrationBuilder.RenameColumn(
                name: "UniqueIdentifier",
                table: "RegulationCategories",
                newName: "Identifier");

            migrationBuilder.RenameColumn(
                name: "UniqueIdentifier",
                table: "GovernmentEntities",
                newName: "Identifier");

            migrationBuilder.RenameColumn(
                name: "UniqueIdentifier",
                table: "Agencies",
                newName: "Identifier");

            migrationBuilder.Sql(
                """
                UPDATE "GovernmentEntities"
                SET "Identifier" = "TitleNumber"::text
                WHERE COALESCE("Identifier", '') = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Agencies"
                SET "Identifier" = "ChapterNumber"
                WHERE COALESCE("Identifier", '') = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "RegulationCategories"
                SET "Identifier" = "SubchapterIdentifier"
                WHERE COALESCE("Identifier", '') = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "RegulationTypes"
                SET "Identifier" = "PartNumber"::text
                WHERE COALESCE("Identifier", '') = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "RegulationSubtypes"
                SET "Identifier" = "SubpartIdentifier"
                WHERE COALESCE("Identifier", '') = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Regulations"
                SET "Identifier" = "SectionNumber"
                WHERE COALESCE("Identifier", '') = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Identifier",
                table: "RegulationTypes",
                newName: "UniqueIdentifier");

            migrationBuilder.RenameColumn(
                name: "Identifier",
                table: "RegulationSubtypes",
                newName: "UniqueIdentifier");

            migrationBuilder.RenameColumn(
                name: "Identifier",
                table: "Regulations",
                newName: "UniqueIdentifier");

            migrationBuilder.RenameColumn(
                name: "Identifier",
                table: "RegulationCategories",
                newName: "UniqueIdentifier");

            migrationBuilder.RenameColumn(
                name: "Identifier",
                table: "GovernmentEntities",
                newName: "UniqueIdentifier");

            migrationBuilder.RenameColumn(
                name: "Identifier",
                table: "Agencies",
                newName: "UniqueIdentifier");
        }
    }
}
