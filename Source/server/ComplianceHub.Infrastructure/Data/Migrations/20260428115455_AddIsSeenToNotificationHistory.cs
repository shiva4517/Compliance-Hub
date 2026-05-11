using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSeenToNotificationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSeen",
                table: "NotificationHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SeenAt",
                table: "NotificationHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_CompanyId_IsSeen",
                table: "NotificationHistory",
                columns: new[] { "CompanyId", "IsSeen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationHistory_CompanyId_IsSeen",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "IsSeen",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "SeenAt",
                table: "NotificationHistory");
        }
    }
}
