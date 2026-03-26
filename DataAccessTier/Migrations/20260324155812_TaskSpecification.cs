using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class TaskSpecification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Memory",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "MemoryPerCPU",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "MemoryPerGPU",
                table: "TaskSpecification");
        }
    }
}
