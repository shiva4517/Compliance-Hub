using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebuildSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CustomerId_GovernmentEntityId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CustomerId_RegulationId",
                table: "Subscriptions");

            migrationBuilder.AddColumn<Guid>(
                name: "AgencyId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationCategoryId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationSubtypeId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RegulationTypeId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscribedNodeName",
                table: "Subscriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubscribingLevel",
                table: "Subscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CustomerId",
                table: "Subscriptions",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CustomerId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RegulationCategoryId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RegulationSubtypeId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RegulationTypeId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "SubscribedNodeName",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "SubscribingLevel",
                table: "Subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CustomerId_GovernmentEntityId",
                table: "Subscriptions",
                columns: new[] { "CustomerId", "GovernmentEntityId" },
                unique: true,
                filter: "\"RegulationId\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CustomerId_RegulationId",
                table: "Subscriptions",
                columns: new[] { "CustomerId", "RegulationId" },
                unique: true,
                filter: "\"RegulationId\" IS NOT NULL AND \"IsDeleted\" = false");
        }
    }
}
