using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class AddingQOSToScheduler : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Code",
                table: "CommandTemplate",
                newName: "ExtendedAllocationCommand");

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
                name: "QualityOfService",
                table: "ClusterNodeType");

            migrationBuilder.RenameColumn(
                name: "ExtendedAllocationCommand",
                table: "CommandTemplate",
                newName: "Code");
        }
    }
}
