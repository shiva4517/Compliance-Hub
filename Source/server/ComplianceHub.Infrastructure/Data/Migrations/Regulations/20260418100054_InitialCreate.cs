using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations.Regulations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GovernmentEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleNumber = table.Column<int>(type: "integer", nullable: false),
                    TitleName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsImported = table.Column<bool>(type: "boolean", nullable: false),
                    LastAmendedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastSyncedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AgencyName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GovernmentEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agencies_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegulationCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubchapterIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubchapterName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GovernmentEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulationCategories_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegulationCategories_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegulationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<int>(type: "integer", nullable: false),
                    PartName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GovernmentEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulationTypes_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegulationTypes_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegulationTypes_RegulationCategories_RegulationCategoryId",
                        column: x => x.RegulationCategoryId,
                        principalTable: "RegulationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegulationSubtypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubpartIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubpartName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RegulationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationSubtypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulationSubtypes_RegulationTypes_RegulationTypeId",
                        column: x => x.RegulationTypeId,
                        principalTable: "RegulationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Regulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SectionName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastAmendedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    GovernmentEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationSubtypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Regulations_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Regulations_GovernmentEntities_GovernmentEntityId",
                        column: x => x.GovernmentEntityId,
                        principalTable: "GovernmentEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Regulations_RegulationCategories_RegulationCategoryId",
                        column: x => x.RegulationCategoryId,
                        principalTable: "RegulationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Regulations_RegulationSubtypes_RegulationSubtypeId",
                        column: x => x.RegulationSubtypeId,
                        principalTable: "RegulationSubtypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Regulations_RegulationTypes_RegulationTypeId",
                        column: x => x.RegulationTypeId,
                        principalTable: "RegulationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegulationChangeHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationChangeHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulationChangeHistory_Regulations_RegulationId",
                        column: x => x.RegulationId,
                        principalTable: "Regulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_GovernmentEntityId_ChapterNumber",
                table: "Agencies",
                columns: new[] { "GovernmentEntityId", "ChapterNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentEntities_TitleNumber",
                table: "GovernmentEntities",
                column: "TitleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_ProcessedAt",
                table: "OutboxEvents",
                column: "ProcessedAt",
                filter: "\"ProcessedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationCategories_AgencyId_SubchapterIdentifier",
                table: "RegulationCategories",
                columns: new[] { "AgencyId", "SubchapterIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegulationCategories_GovernmentEntityId",
                table: "RegulationCategories",
                column: "GovernmentEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationChangeHistory_RegulationId_Version",
                table: "RegulationChangeHistory",
                columns: new[] { "RegulationId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_AgencyId",
                table: "Regulations",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_GovernmentEntityId",
                table: "Regulations",
                column: "GovernmentEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_RegulationCategoryId",
                table: "Regulations",
                column: "RegulationCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_RegulationSubtypeId",
                table: "Regulations",
                column: "RegulationSubtypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Regulations_RegulationTypeId_SectionNumber",
                table: "Regulations",
                columns: new[] { "RegulationTypeId", "SectionNumber" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationSubtypes_RegulationTypeId_SubpartIdentifier",
                table: "RegulationSubtypes",
                columns: new[] { "RegulationTypeId", "SubpartIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegulationTypes_AgencyId",
                table: "RegulationTypes",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationTypes_GovernmentEntityId",
                table: "RegulationTypes",
                column: "GovernmentEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_RegulationTypes_RegulationCategoryId_PartNumber",
                table: "RegulationTypes",
                columns: new[] { "RegulationCategoryId", "PartNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "RegulationChangeHistory");

            migrationBuilder.DropTable(
                name: "Regulations");

            migrationBuilder.DropTable(
                name: "RegulationSubtypes");

            migrationBuilder.DropTable(
                name: "RegulationTypes");

            migrationBuilder.DropTable(
                name: "RegulationCategories");

            migrationBuilder.DropTable(
                name: "Agencies");

            migrationBuilder.DropTable(
                name: "GovernmentEntities");
        }
    }
}
