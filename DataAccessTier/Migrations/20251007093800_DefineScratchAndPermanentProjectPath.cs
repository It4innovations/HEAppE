using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class DefineScratchAndPermanentProjectPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocalBasepath",
                table: "ClusterProject",
                newName: "ScratchStoragePath");

            migrationBuilder.AddColumn<string>(
                name: "PermanentStoragePath",
                table: "ClusterProject",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermanentStoragePath",
                table: "ClusterProject");

            migrationBuilder.RenameColumn(
                name: "ScratchStoragePath",
                table: "ClusterProject",
                newName: "LocalBasepath");
        }
    }
}
