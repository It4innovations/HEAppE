using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class ModificationOpenStack : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdministrationUserRole");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentials");

            migrationBuilder.DropTable(
                name: "AdministrationRole");

            migrationBuilder.DropTable(
                name: "AdministrationUser");

            migrationBuilder.DropColumn(
                name: "Domain",
                table: "OpenStackInstance");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "OpenStackInstance");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AdaptorUser",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "AdaptorUser",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredential",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredential", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackDomain",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OpenStackInstanceId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackDomain_OpenStackInstance_OpenStackInstanceId",
                        column: x => x.OpenStackInstanceId,
                        principalTable: "OpenStackInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredentialDomain",
                columns: table => new
                {
                    OpenStackAuthenticationCredentialId = table.Column<long>(type: "bigint", nullable: false),
                    OpenStackDomainId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredentialDomain", x => new { x.OpenStackAuthenticationCredentialId, x.OpenStackDomainId });
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialDomain_OpenStackAuthenticationCredential_OpenStackAuthenticationCredentialId",
                        column: x => x.OpenStackAuthenticationCredentialId,
                        principalTable: "OpenStackAuthenticationCredential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialDomain_OpenStackDomain_OpenStackDomainId",
                        column: x => x.OpenStackDomainId,
                        principalTable: "OpenStackDomain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackProject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UID = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OpenStackDomainId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackProject_OpenStackDomain_OpenStackDomainId",
                        column: x => x.OpenStackDomainId,
                        principalTable: "OpenStackDomain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackProjectDomain",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UID = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OpenStackProjectId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackProjectDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackProjectDomain_OpenStackProject_OpenStackProjectId",
                        column: x => x.OpenStackProjectId,
                        principalTable: "OpenStackProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredentialProjectDomain",
                columns: table => new
                {
                    OpenStackAuthenticationCredentialId = table.Column<long>(type: "bigint", nullable: false),
                    OpenStackProjectDomainId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredentialProjectDomain", x => new { x.OpenStackAuthenticationCredentialId, x.OpenStackProjectDomainId });
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialProjectDomain_OpenStackAuthenticationCredential_OpenStackAuthenticationCredentialId",
                        column: x => x.OpenStackAuthenticationCredentialId,
                        principalTable: "OpenStackAuthenticationCredential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialProjectDomain_OpenStackProjectDomain_OpenStackProjectDomainId",
                        column: x => x.OpenStackProjectDomainId,
                        principalTable: "OpenStackProjectDomain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentialDomain_OpenStackDomainId",
                table: "OpenStackAuthenticationCredentialDomain",
                column: "OpenStackDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentialProjectDomain_OpenStackProjectDomainId",
                table: "OpenStackAuthenticationCredentialProjectDomain",
                column: "OpenStackProjectDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackDomain_OpenStackInstanceId",
                table: "OpenStackDomain",
                column: "OpenStackInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackProject_OpenStackDomainId",
                table: "OpenStackProject",
                column: "OpenStackDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackProjectDomain_OpenStackProjectId",
                table: "OpenStackProjectDomain",
                column: "OpenStackProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentialDomain");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentialProjectDomain");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredential");

            migrationBuilder.DropTable(
                name: "OpenStackProjectDomain");

            migrationBuilder.DropTable(
                name: "OpenStackProject");

            migrationBuilder.DropTable(
                name: "OpenStackDomain");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AdaptorUser");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "AdaptorUser");

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "OpenStackInstance",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "OpenStackInstance",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdministrationRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrationRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdministrationUser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LanguageId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrationUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdministrationUser_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpenStackInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "AdministrationUserRole",
                columns: table => new
                {
                    AdministrationRoleId = table.Column<long>(type: "bigint", nullable: false),
                    AdministrationUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrationUserRole", x => new { x.AdministrationRoleId, x.AdministrationUserId });
                    table.ForeignKey(
                        name: "FK_AdministrationUserRole_AdministrationRole_AdministrationRoleId",
                        column: x => x.AdministrationRoleId,
                        principalTable: "AdministrationRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdministrationUserRole_AdministrationUser_AdministrationUserId",
                        column: x => x.AdministrationUserId,
                        principalTable: "AdministrationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrationUser_LanguageId",
                table: "AdministrationUser",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrationUserRole_AdministrationUserId",
                table: "AdministrationUserRole",
                column: "AdministrationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentials_OpenStackInstanceId",
                table: "OpenStackAuthenticationCredentials",
                column: "OpenStackInstanceId");
        }
    }
}
