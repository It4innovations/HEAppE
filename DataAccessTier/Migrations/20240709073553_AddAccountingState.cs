using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResourceConsumed",
                table: "SubmittedTaskInfo");

            migrationBuilder.CreateTable(
                name: "AccountingState",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingStateType = table.Column<int>(type: "int", nullable: false),
                    ComputingStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComputingEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingState", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingState_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumedResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmittedTaskInfoId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumedResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumedResources_Accounting_AccountingId",
                        column: x => x.AccountingId,
                        principalTable: "Accounting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumedResources_SubmittedTaskInfo_SubmittedTaskInfoId",
                        column: x => x.SubmittedTaskInfoId,
                        principalTable: "SubmittedTaskInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingState_ProjectId",
                table: "AccountingState",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedResources_AccountingId",
                table: "ConsumedResources",
                column: "AccountingId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedResources_SubmittedTaskInfoId",
                table: "ConsumedResources",
                column: "SubmittedTaskInfoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingState");

            migrationBuilder.DropTable(
                name: "ConsumedResources");

            migrationBuilder.AddColumn<double>(
                name: "ResourceConsumed",
                table: "SubmittedTaskInfo",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
