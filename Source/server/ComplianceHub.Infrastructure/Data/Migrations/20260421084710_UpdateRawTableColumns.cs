using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRawTableColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationHistory_CustomerId_IsRead",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "ChangeNoticeId",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ChangeNotices");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ChangeNotices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ChangeNotices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_CustomerId_Status",
                table: "NotificationHistory",
                columns: new[] { "CustomerId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationHistory_CustomerId_Status",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ChangeNotices");

            migrationBuilder.AddColumn<Guid>(
                name: "ChangeNoticeId",
                table: "NotificationHistory",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStatus",
                table: "NotificationHistory",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "NotificationHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "NotificationHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ChangeNotices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_CustomerId_IsRead",
                table: "NotificationHistory",
                columns: new[] { "CustomerId", "IsRead" });
        }
    }
}
