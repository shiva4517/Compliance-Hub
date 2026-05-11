using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGrievancesFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Grievances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBySecurityUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecipientSecurityUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grievances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grievances_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grievances_SecurityUsers_CreatedBySecurityUserId",
                        column: x => x.CreatedBySecurityUserId,
                        principalTable: "SecurityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grievances_SecurityUsers_RecipientSecurityUserId",
                        column: x => x.RecipientSecurityUserId,
                        principalTable: "SecurityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Grievances_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GrievanceReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrievanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderSecurityUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrievanceReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrievanceReplies_Grievances_GrievanceId",
                        column: x => x.GrievanceId,
                        principalTable: "Grievances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GrievanceReplies_SecurityUsers_SenderSecurityUserId",
                        column: x => x.SenderSecurityUserId,
                        principalTable: "SecurityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceReplies_GrievanceId",
                table: "GrievanceReplies",
                column: "GrievanceId");

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceReplies_GrievanceId_CreatedAt",
                table: "GrievanceReplies",
                columns: new[] { "GrievanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceReplies_SenderSecurityUserId",
                table: "GrievanceReplies",
                column: "SenderSecurityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_CompanyId",
                table: "Grievances",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_CompanyId_Status",
                table: "Grievances",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_CreatedBySecurityUserId",
                table: "Grievances",
                column: "CreatedBySecurityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_LastMessageAt",
                table: "Grievances",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_RecipientSecurityUserId",
                table: "Grievances",
                column: "RecipientSecurityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_SubscriptionId",
                table: "Grievances",
                column: "SubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrievanceReplies");

            migrationBuilder.DropTable(
                name: "Grievances");
        }
    }
}
