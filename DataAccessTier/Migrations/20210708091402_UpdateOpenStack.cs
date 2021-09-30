using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class UpdateOpenStack : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "OpenStackInstance",
                type: "nvarchar(40)",
                maxLength: 40,
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
                name: "Project",
                table: "OpenStackInstance");
        }
    }
}
