using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailUniquenessConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityUsers_Email",
                table: "SecurityUsers");

            migrationBuilder.DropIndex(
                name: "IX_Employees_PrimaryEmail",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityUsers_Email",
                table: "SecurityUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_PrimaryEmail",
                table: "Employees",
                columns: new[] { "CompanyId", "PrimaryEmail" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityUsers_Email",
                table: "SecurityUsers");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId_PrimaryEmail",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityUsers_Email",
                table: "SecurityUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PrimaryEmail",
                table: "Employees",
                column: "PrimaryEmail",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
