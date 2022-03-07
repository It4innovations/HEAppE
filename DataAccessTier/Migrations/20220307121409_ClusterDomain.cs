using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class ClusterDomain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllocatedCores",
                table: "SubmittedTaskInfo",
                type: "int",
                nullable: true);

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
                name: "DomainName",
                table: "Cluster");
        }
    }
}
