using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class SubProjectExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubProjectId",
                table: "JobSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubProject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubProject_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_SubProjectId",
                table: "JobSpecification",
                column: "SubProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_Identifier_ProjectId",
                table: "SubProject",
                columns: new[] { "Identifier", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_ProjectId",
                table: "SubProject",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_SubProject_SubProjectId",
                table: "JobSpecification",
                column: "SubProjectId",
                principalTable: "SubProject",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_SubProject_SubProjectId",
                table: "JobSpecification");

            migrationBuilder.DropTable(
                name: "SubProject");

            migrationBuilder.DropIndex(
                name: "IX_JobSpecification_SubProjectId",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "SubProjectId",
                table: "JobSpecification");
        }
    }
}
