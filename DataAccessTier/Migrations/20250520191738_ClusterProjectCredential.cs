using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class ClusterProjectCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AdaptorUserId",
                table: "ClusterProjectCredentials",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentials_AdaptorUserId",
                table: "ClusterProjectCredentials",
                column: "AdaptorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterProjectCredentials_AdaptorUser_AdaptorUserId",
                table: "ClusterProjectCredentials",
                column: "AdaptorUserId",
                principalTable: "AdaptorUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClusterProjectCredentials_AdaptorUser_AdaptorUserId",
                table: "ClusterProjectCredentials");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProjectCredentials_AdaptorUserId",
                table: "ClusterProjectCredentials");

            migrationBuilder.DropColumn(
                name: "AdaptorUserId",
                table: "ClusterProjectCredentials");
        }
    }
}
