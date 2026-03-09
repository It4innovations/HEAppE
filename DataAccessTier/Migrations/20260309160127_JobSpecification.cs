using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class JobSpecification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Memory",
                table: "JobSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MemoryPerCPU",
                table: "JobSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MemoryPerGPU",
                table: "JobSpecification",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Memory",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "MemoryPerCPU",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "MemoryPerGPU",
                table: "JobSpecification");
        }
    }
}
