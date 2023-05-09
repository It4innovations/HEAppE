using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class AddingQOSToScheduler : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "CommandTemplate");

            migrationBuilder.AddColumn<string>(
                name: "ExtendedAllocationCommand",
                table: "CommandTemplate",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityOfService",
                table: "ClusterNodeType",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtendedAllocationCommand",
                table: "CommandTemplate");

            migrationBuilder.DropColumn(
                name: "QualityOfService",
                table: "ClusterNodeType");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "CommandTemplate",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
