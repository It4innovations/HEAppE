using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class LocalProcessIdSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalProcessId",
                table: "SubmittedTaskInfo",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AdaptorUserRole_Name",
                table: "AdaptorUserRole",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_AdaptorUserRole_Name",
                table: "AdaptorUserRole");

            migrationBuilder.DropColumn(
                name: "LocalProcessId",
                table: "SubmittedTaskInfo");
        }
    }
}
