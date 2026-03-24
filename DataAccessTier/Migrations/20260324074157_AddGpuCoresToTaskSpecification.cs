using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddGpuCoresToTaskSpecification : Migration
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
        }
    }
}
