using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class ClusterDomain_CommandTemplateIsEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllocatedCores",
                table: "SubmittedTaskInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "CommandTemplate",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DomainName",
                table: "Cluster",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocatedCores",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "CommandTemplate");

            migrationBuilder.DropColumn(
                name: "DomainName",
                table: "Cluster");
        }
    }
}
