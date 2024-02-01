using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class UserRoleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "MaxNodesPerJob",
                table: "ClusterNodeType",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxNodesPerUser",
                table: "ClusterNodeType",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AdaptorUser",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "UserType",
                table: "AdaptorUser",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxNodesPerJob",
                table: "ClusterNodeType");

            migrationBuilder.DropColumn(
                name: "MaxNodesPerUser",
                table: "ClusterNodeType");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "AdaptorUser");

            migrationBuilder.AddColumn<long>(
                name: "ParentRoleId",
                table: "AdaptorUserRole",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AdaptorUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

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
    }
}
