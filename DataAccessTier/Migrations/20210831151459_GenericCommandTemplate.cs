using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class GenericCommandTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "CommandTemplateParameter",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGeneric",
                table: "CommandTemplate",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "CommandTemplateParameter");

            migrationBuilder.DropColumn(
                name: "IsGeneric",
                table: "CommandTemplate");
        }
    }
}
