using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class FirecRestEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FirecRestEndpointId",
                table: "Cluster",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FirecRestEndpoint",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IdpUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirecRestEndpoint", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cluster_FirecRestEndpointId",
                table: "Cluster",
                column: "FirecRestEndpointId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cluster_FirecRestEndpoint_FirecRestEndpointId",
                table: "Cluster",
                column: "FirecRestEndpointId",
                principalTable: "FirecRestEndpoint",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cluster_FirecRestEndpoint_FirecRestEndpointId",
                table: "Cluster");

            migrationBuilder.DropTable(
                name: "FirecRestEndpoint");

            migrationBuilder.DropIndex(
                name: "IX_Cluster_FirecRestEndpointId",
                table: "Cluster");

            migrationBuilder.DropColumn(
                name: "FirecRestEndpointId",
                table: "Cluster");
        }
    }
}
