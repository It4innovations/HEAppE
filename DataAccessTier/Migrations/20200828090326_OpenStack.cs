using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class OpenStack : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenStackInstance",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    InstanceUrl = table.Column<string>(maxLength: 70, nullable: false),
                    Domain = table.Column<string>(maxLength: 70, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackInstance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackSession",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ApplicationCredentialsId = table.Column<string>(nullable: false),
                    ApplicationCredentialsSecret = table.Column<string>(nullable: false),
                    AuthenticationTime = table.Column<DateTime>(nullable: false),
                    ExpirationTime = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackSession_AdaptorUser_UserId",
                        column: x => x.UserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(maxLength: 50, nullable: false),
                    Password = table.Column<string>(maxLength: 50, nullable: false),
                    OpenStackInstanceId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentials_OpenStackInstance_OpenStackInstanceId",
                        column: x => x.OpenStackInstanceId,
                        principalTable: "OpenStackInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentials_OpenStackInstanceId",
                table: "OpenStackAuthenticationCredentials",
                column: "OpenStackInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackSession_UserId",
                table: "OpenStackSession",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentials");

            migrationBuilder.DropTable(
                name: "OpenStackSession");

            migrationBuilder.DropTable(
                name: "OpenStackInstance");
        }
    }
}
