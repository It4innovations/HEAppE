using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class ImprovedAccountingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubProjectId",
                table: "JobSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ClusterNodeTypeAggregationId",
                table: "ClusterNodeType",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "Accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Formula = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ValidityFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClusterNodeTypeAggregation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AllocationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ValidityFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeTypeAggregation", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "ClusterNodeTypeAggregationAccounting",
                columns: table => new
                {
                    ClusterNodeTypeAggregationId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeTypeAggregationAccounting", x => new { x.ClusterNodeTypeAggregationId, x.AccountingId });
                    table.ForeignKey(
                        name: "FK_ClusterNodeTypeAggregationAccounting_Accounting_AccountingId",
                        column: x => x.AccountingId,
                        principalTable: "Accounting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClusterNodeTypeAggregationAccounting_ClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                        column: x => x.ClusterNodeTypeAggregationId,
                        principalTable: "ClusterNodeTypeAggregation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectClusterNodeTypeAggregation",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ClusterNodeTypeAggregationId = table.Column<long>(type: "bigint", nullable: false),
                    AllocationAmount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectClusterNodeTypeAggregation", x => new { x.ProjectId, x.ClusterNodeTypeAggregationId });
                    table.ForeignKey(
                        name: "FK_ProjectClusterNodeTypeAggregation_ClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                        column: x => x.ClusterNodeTypeAggregationId,
                        principalTable: "ClusterNodeTypeAggregation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectClusterNodeTypeAggregation_Project_ProjectId",
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
                name: "IX_ClusterNodeType_ClusterNodeTypeAggregationId",
                table: "ClusterNodeType",
                column: "ClusterNodeTypeAggregationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_AccountingId",
                table: "ClusterNodeTypeAggregationAccounting",
                column: "AccountingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                table: "ProjectClusterNodeTypeAggregation",
                column: "ClusterNodeTypeAggregationId");

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
                name: "FK_ClusterNodeType_ClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                table: "ClusterNodeType",
                column: "ClusterNodeTypeAggregationId",
                principalTable: "ClusterNodeTypeAggregation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_ClusterNodeType_ClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                table: "ClusterNodeType");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_SubProject_SubProjectId",
                table: "JobSpecification");

            migrationBuilder.DropTable(
                name: "ClusterNodeTypeAggregationAccounting");

            migrationBuilder.DropTable(
                name: "ProjectClusterNodeTypeAggregation");

            migrationBuilder.DropTable(
                name: "SubProject");

            migrationBuilder.DropTable(
                name: "Accounting");

            migrationBuilder.DropTable(
                name: "ClusterNodeTypeAggregation");

            migrationBuilder.DropIndex(
                name: "IX_JobSpecification_SubProjectId",
                table: "JobSpecification");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeType_ClusterNodeTypeAggregationId",
                table: "ClusterNodeType");

            migrationBuilder.DropColumn(
                name: "SubProjectId",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "ClusterNodeTypeAggregationId",
                table: "ClusterNodeType");
        }
    }
}
