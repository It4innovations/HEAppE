using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class ClusterProxy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProxyConnectionId",
                table: "Cluster",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClusterProxyConnection",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Host = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterProxyConnection", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cluster_ProxyConnectionId",
                table: "Cluster",
                column: "ProxyConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cluster_ClusterProxyConnection_ProxyConnectionId",
                table: "Cluster",
                column: "ProxyConnectionId",
                principalTable: "ClusterProxyConnection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cluster_ClusterProxyConnection_ProxyConnectionId",
                table: "Cluster");

            migrationBuilder.DropTable(
                name: "ClusterProxyConnection");

            migrationBuilder.DropIndex(
                name: "IX_Cluster_ProxyConnectionId",
                table: "Cluster");

            migrationBuilder.DropColumn(
                name: "ProxyConnectionId",
                table: "Cluster");
        }
    }
}
