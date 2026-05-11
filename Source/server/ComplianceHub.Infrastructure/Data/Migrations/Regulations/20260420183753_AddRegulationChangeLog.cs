using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations.Regulations
{
    /// <inheritdoc />
    public partial class AddRegulationChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegulationChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GovernmentEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    GovernmentEntityName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgencyName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RegulationCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationCategoryName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RegulationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationTypeName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RegulationSubtypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegulationSubtypeName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SectionNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SectionTitle = table.Column<string>(type: "text", nullable: false),
                    PreviousContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NewContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PreviousHtmlContent = table.Column<string>(type: "text", nullable: true),
                    NewHtmlContent = table.Column<string>(type: "text", nullable: true),
                    PreviousAmendedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NewAmendedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ArchivedVersion = table.Column<int>(type: "integer", nullable: false),
                    NewVersion = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulationChangeLogs_Regulations_RegulationId",
                        column: x => x.RegulationId,
                        principalTable: "Regulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegulationChangeLogs_ChangedAt",
                table: "RegulationChangeLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationChangeLogs_GovernmentEntityId",
                table: "RegulationChangeLogs",
                column: "GovernmentEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationChangeLogs_RegulationId",
                table: "RegulationChangeLogs",
                column: "RegulationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegulationChangeLogs");
        }
    }
}
