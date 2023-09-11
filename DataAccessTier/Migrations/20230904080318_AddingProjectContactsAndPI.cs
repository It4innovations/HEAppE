using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddingProjectContactsAndPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdaptorUser_Language_LanguageId",
                table: "AdaptorUser");

            migrationBuilder.DropTable(
                name: "MessageLocalization");

            migrationBuilder.DropTable(
                name: "MessageTemplateParameter");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "ResourceLimitation");

            migrationBuilder.DropTable(
                name: "Language");

            migrationBuilder.DropTable(
                name: "MessageTemplate");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUser_LanguageId",
                table: "AdaptorUser");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "AdaptorUser");

            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicKey = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectContact",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    IsPI = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectContact", x => new { x.ProjectId, x.ContactId });
                    table.ForeignKey(
                        name: "FK_ProjectContact_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectContact_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContact_ContactId",
                table: "ProjectContact",
                column: "ContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectContact");

            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.AddColumn<long>(
                name: "LanguageId",
                table: "AdaptorUser",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Language",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsoCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Language", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Event = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceLimitation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeTypeId = table.Column<long>(type: "bigint", nullable: true),
                    AdaptorUserId = table.Column<long>(type: "bigint", nullable: true),
                    MaxCoresPerJob = table.Column<int>(type: "int", nullable: true),
                    TotalMaxCores = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLimitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceLimitation_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLimitation_ClusterNodeType_NodeTypeId",
                        column: x => x.NodeTypeId,
                        principalTable: "ClusterNodeType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MessageLocalization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageId = table.Column<long>(type: "bigint", nullable: true),
                    LocalizedHeader = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocalizedText = table.Column<string>(type: "text", nullable: false),
                    MessageTemplateId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLocalization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageLocalization_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MessageLocalization_MessageTemplate_MessageTemplateId",
                        column: x => x.MessageTemplateId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplateParameter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identifier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MessageTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    Query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplateParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageTemplateParameter_MessageTemplate_MessageTemplateId",
                        column: x => x.MessageTemplateId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageId = table.Column<long>(type: "bigint", nullable: true),
                    MessageTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Header = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurrenceTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SentTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notification_MessageTemplate_MessageTemplateId",
                        column: x => x.MessageTemplateId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_LanguageId",
                table: "AdaptorUser",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLocalization_LanguageId",
                table: "MessageLocalization",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLocalization_MessageTemplateId",
                table: "MessageLocalization",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplateParameter_MessageTemplateId",
                table: "MessageTemplateParameter",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_LanguageId",
                table: "Notification",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_MessageTemplateId",
                table: "Notification",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLimitation_AdaptorUserId",
                table: "ResourceLimitation",
                column: "AdaptorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLimitation_NodeTypeId",
                table: "ResourceLimitation",
                column: "NodeTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptorUser_Language_LanguageId",
                table: "AdaptorUser",
                column: "LanguageId",
                principalTable: "Language",
                principalColumn: "Id");
        }
    }
}
