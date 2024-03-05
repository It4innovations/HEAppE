using Microsoft.EntityFrameworkCore.Migrations;


namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class RoleParentModification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentRoleId",
                table: "AdaptorUserRole",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserRole_ParentRoleId",
                table: "AdaptorUserRole",
                column: "ParentRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptorUserRole_AdaptorUserRole_ParentRoleId",
                table: "AdaptorUserRole",
                column: "ParentRoleId",
                principalTable: "AdaptorUserRole",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdaptorUserRole_AdaptorUserRole_ParentRoleId",
                table: "AdaptorUserRole");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUserRole_ParentRoleId",
                table: "AdaptorUserRole");

            migrationBuilder.DropColumn(
                name: "ParentRoleId",
                table: "AdaptorUserRole");
        }
    }
}
