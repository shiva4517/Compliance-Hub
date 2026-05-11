using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceARMCustomerWithCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_ARMCustomers_CustomerId",
                table: "NotificationHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_ARMCustomers_CustomerId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "ARMCustomers");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryContactFirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryContactLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecondaryEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MobileNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PrimaryAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PrimaryCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PrimaryState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PrimaryPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SecondaryAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SecondaryCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SecondaryState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SecondaryPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_CustomerCode",
                table: "Customers",
                columns: new[] { "CompanyId", "CustomerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_PrimaryEmail",
                table: "Customers",
                columns: new[] { "CompanyId", "PrimaryEmail" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_Customers_CustomerId",
                table: "NotificationHistory",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Customers_CustomerId",
                table: "Subscriptions",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistory_Customers_CustomerId",
                table: "NotificationHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Customers_CustomerId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.CreateTable(
                name: "ARMCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AemsliteCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    AemsmailActive = table.Column<bool>(type: "boolean", nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DatabaseName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateInstalled = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    SchemaVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SiteInProduction = table.Column<bool>(type: "boolean", nullable: false),
                    SiteNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    WebVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ARMCustomers", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistory_ARMCustomers_CustomerId",
                table: "NotificationHistory",
                column: "CustomerId",
                principalTable: "ARMCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_ARMCustomers_CustomerId",
                table: "Subscriptions",
                column: "CustomerId",
                principalTable: "ARMCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
