using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegulationDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegulationDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Condition = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SuggestedTask = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FrequencyTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDateTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulationDetails_DueDateTypes_DueDateTypeId",
                        column: x => x.DueDateTypeId,
                        principalTable: "DueDateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RegulationDetails_FrequencyTypes_FrequencyTypeId",
                        column: x => x.FrequencyTypeId,
                        principalTable: "FrequencyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegulationDetails_DueDateTypeId",
                table: "RegulationDetails",
                column: "DueDateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationDetails_FrequencyTypeId",
                table: "RegulationDetails",
                column: "FrequencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationDetails_RegulationId",
                table: "RegulationDetails",
                column: "RegulationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegulationDetails");
        }
    }
}
