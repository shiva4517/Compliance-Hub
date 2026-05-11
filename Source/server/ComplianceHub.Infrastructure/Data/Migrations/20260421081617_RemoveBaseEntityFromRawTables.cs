using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBaseEntityFromRawTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "NotificationHistory");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ChangeNotices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "NotificationHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "NotificationHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "NotificationHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ChangeNotices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChangeNotices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ChangeNotices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ChangeNotices",
                type: "text",
                nullable: true);
        }
    }
}
