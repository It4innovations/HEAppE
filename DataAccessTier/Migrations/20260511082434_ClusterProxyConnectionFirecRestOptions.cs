using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class ClusterProxyConnectionFirecRestOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirecRestOptions",
                table: "ClusterProxyConnection",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirecRestOptions",
                table: "ClusterProxyConnection");
        }
    }
}
