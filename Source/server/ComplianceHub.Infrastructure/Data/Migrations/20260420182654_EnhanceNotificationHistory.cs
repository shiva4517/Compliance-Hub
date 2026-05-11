using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceNotificationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgencyId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgencyName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GovernmentEntityId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GovernmentEntityName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNotified",
                table: "NotificationHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetryAt",
                table: "NotificationHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PresentRegulationId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentRegulationName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousRegulationId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousRegulationName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientEmail",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationCategoryId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulationCategoryName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationSubtypeId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulationSubtypeName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationTypeId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulationTypeName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "NotificationHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SenderEmail",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "NotificationHistory",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_CompanyId",
                table: "NotificationHistory",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_CompanyId_Status",
                table: "NotificationHistory",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_Companies_CompanyId",
                table: "NotificationHistory",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_Companies_CompanyId",
                table: "NotificationHistory");

            migrationBuilder.DropIndex(
                name: "IX_NotificationHistory_CompanyId",
                table: "NotificationHistory");

            migrationBuilder.DropIndex(
                name: "IX_NotificationHistory_CompanyId_Status",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "AgencyName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "GovernmentEntityId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "GovernmentEntityName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "IsNotified",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "LastRetryAt",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "PresentRegulationId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "PresentRegulationName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "PreviousRegulationId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "PreviousRegulationName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RecipientEmail",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RegulationCategoryId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RegulationCategoryName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RegulationSubtypeId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RegulationSubtypeName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RegulationTypeId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RegulationTypeName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "SenderEmail",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "NotificationHistory");
        }
    }
}
