using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebuildChangeNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_ChangeNotices_ChangeNoticeId",
                table: "NotificationHistory");

            migrationBuilder.DropIndex(
                name: "IX_NotificationHistory_ChangeNoticeId",
                table: "NotificationHistory");

            migrationBuilder.DropIndex(
                name: "IX_ChangeNotices_NoticeDate",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "NoticeDate",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ChangeNotices");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "ChangeNotices",
                newName: "SectionTitle");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "NotificationHistory",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Guid>(
                name: "AgencyId",
                table: "ChangeNotices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgencyName",
                table: "ChangeNotices",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ArchivedVersion",
                table: "ChangeNotices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangedAt",
                table: "ChangeNotices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "GovernmentEntityName",
                table: "ChangeNotices",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "NewAmendedDate",
                table: "ChangeNotices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewContentHash",
                table: "ChangeNotices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewHtmlContent",
                table: "ChangeNotices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewVersion",
                table: "ChangeNotices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PreviousAmendedDate",
                table: "ChangeNotices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousContentHash",
                table: "ChangeNotices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousHtmlContent",
                table: "ChangeNotices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationCategoryId",
                table: "ChangeNotices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulationCategoryName",
                table: "ChangeNotices",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationSubtypeId",
                table: "ChangeNotices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulationSubtypeName",
                table: "ChangeNotices",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationTypeId",
                table: "ChangeNotices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegulationTypeName",
                table: "ChangeNotices",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SectionNumber",
                table: "ChangeNotices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotices_AgencyId",
                table: "ChangeNotices",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotices_ChangedAt",
                table: "ChangeNotices",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotices_GovernmentEntityId",
                table: "ChangeNotices",
                column: "GovernmentEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotices_RegulationId",
                table: "ChangeNotices",
                column: "RegulationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChangeNotices_AgencyId",
                table: "ChangeNotices");

            migrationBuilder.DropIndex(
                name: "IX_ChangeNotices_ChangedAt",
                table: "ChangeNotices");

            migrationBuilder.DropIndex(
                name: "IX_ChangeNotices_GovernmentEntityId",
                table: "ChangeNotices");

            migrationBuilder.DropIndex(
                name: "IX_ChangeNotices_RegulationId",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "AgencyName",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "ArchivedVersion",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "ChangedAt",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "GovernmentEntityName",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "NewAmendedDate",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "NewContentHash",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "NewHtmlContent",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "NewVersion",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "PreviousAmendedDate",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "PreviousContentHash",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "PreviousHtmlContent",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "RegulationCategoryId",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "RegulationCategoryName",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "RegulationSubtypeId",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "RegulationSubtypeName",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "RegulationTypeId",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "RegulationTypeName",
                table: "ChangeNotices");

            migrationBuilder.DropColumn(
                name: "SectionNumber",
                table: "ChangeNotices");

            migrationBuilder.RenameColumn(
                name: "SectionTitle",
                table: "ChangeNotices",
                newName: "Summary");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "NotificationHistory",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "ChangeNotices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NoticeDate",
                table: "ChangeNotices",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ChangeNotices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_ChangeNoticeId",
                table: "NotificationHistory",
                column: "ChangeNoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeNotices_NoticeDate",
                table: "ChangeNotices",
                column: "NoticeDate");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_ChangeNotices_ChangeNoticeId",
                table: "NotificationHistory",
                column: "ChangeNoticeId",
                principalTable: "ChangeNotices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
