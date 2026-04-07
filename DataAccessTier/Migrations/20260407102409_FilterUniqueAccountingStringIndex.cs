using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class FilterUniqueAccountingStringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubProject_Identifier_ProjectId",
                table: "SubProject");

            migrationBuilder.DropIndex(
                name: "IX_Project_AccountingString",
                table: "Project");

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_Identifier_ProjectId",
                table: "SubProject",
                columns: new[] { "Identifier", "ProjectId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Project_AccountingString",
                table: "Project",
                column: "AccountingString",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubProject_Identifier_ProjectId",
                table: "SubProject");

            migrationBuilder.DropIndex(
                name: "IX_Project_AccountingString",
                table: "Project");

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_Identifier_ProjectId",
                table: "SubProject",
                columns: new[] { "Identifier", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_AccountingString",
                table: "Project",
                column: "AccountingString",
                unique: true);
        }
    }
}
