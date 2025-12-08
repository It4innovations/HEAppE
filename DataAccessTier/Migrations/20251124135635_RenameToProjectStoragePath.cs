using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class RenameToProjectStoragePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PermanentStoragePath",
                table: "ClusterProject",
                newName: "ProjectStoragePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProjectStoragePath",
                table: "ClusterProject",
                newName: "PermanentStoragePath");
        }
    }
}
