using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredAuthTypeToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GpuCores",
                table: "TaskSpecification",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GpuNodes",
                table: "TaskSpecification",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Memory",
                table: "TaskSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MemoryPerCPU",
                table: "TaskSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MemoryPerGPU",
                table: "TaskSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredAuthType",
                table: "Project",
                type: "int",
                nullable: true,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "PreferredAuthType",
                table: "ClusterProject",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GpuCores",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "GpuNodes",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "Memory",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "MemoryPerCPU",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "MemoryPerGPU",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "PreferredAuthType",
                table: "Project");

            migrationBuilder.DropColumn(
                name: "PreferredAuthType",
                table: "ClusterProject");
        }
    }
}
