using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeesAndRefactorSecurityUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecurityUsers_ARMCustomers_CustomerId",
                table: "SecurityUsers");

            migrationBuilder.DropIndex(
                name: "IX_SecurityUsers_CustomerId",
                table: "SecurityUsers");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "SecurityUsers",
                newName: "UserId");

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "SecurityUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecondaryEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MobileNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CompanyDivisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyDistrictId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PrimaryCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrimaryPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SecondaryAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SecondaryCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SecondaryState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SecondaryPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_CompanyDivisions_CompanyDivisionId",
                        column: x => x.CompanyDivisionId,
                        principalTable: "CompanyDivisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyDivisionId",
                table: "Employees",
                column: "CompanyDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_EmployeeCode",
                table: "Employees",
                columns: new[] { "CompanyId", "EmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PrimaryEmail",
                table: "Employees",
                column: "PrimaryEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "SecurityUsers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "SecurityUsers",
                newName: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityUsers_CustomerId",
                table: "SecurityUsers",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityUsers_ARMCustomers_CustomerId",
                table: "SecurityUsers",
                column: "CustomerId",
                principalTable: "ARMCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
