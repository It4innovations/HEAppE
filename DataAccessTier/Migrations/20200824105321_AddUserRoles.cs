using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class AddUserRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdaptorUserRole",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUserUserRole",
                columns: table => new
                {
                    AdaptorUserId = table.Column<long>(nullable: false),
                    AdaptorUserRoleId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserUserRole", x => new { x.AdaptorUserId, x.AdaptorUserRoleId });
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserRole_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserRole_AdaptorUserRole_AdaptorUserRoleId",
                        column: x => x.AdaptorUserRoleId,
                        principalTable: "AdaptorUserRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserRole_AdaptorUserRoleId",
                table: "AdaptorUserUserRole",
                column: "AdaptorUserRoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdaptorUserUserRole");

            migrationBuilder.DropTable(
                name: "AdaptorUserRole");
        }
    }
}
