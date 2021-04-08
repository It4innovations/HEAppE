using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class TaskParalizationSpec : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredNodes",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "AllocatedCoreIds",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "RequestedNodeGroups",
                table: "ClusterNodeType");

            migrationBuilder.AlterColumn<string>(
                name: "JobArrays",
                table: "TaskSpecification",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlacementPolicy",
                table: "TaskSpecification",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClusterNodeTypeRequestedGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 100, nullable: true),
                    ClusterNodeTypeId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeTypeRequestedGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterNodeTypeRequestedGroup_ClusterNodeType_ClusterNodeTypeId",
                        column: x => x.ClusterNodeTypeId,
                        principalTable: "ClusterNodeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmittedTaskAllocationNodeInfo",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AllocationNodeId = table.Column<string>(maxLength: 50, nullable: false),
                    SubmittedTaskInfoId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedTaskAllocationNodeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfo_SubmittedTaskInfoId",
                        column: x => x.SubmittedTaskInfoId,
                        principalTable: "SubmittedTaskInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskParalizationSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MaxCores = table.Column<int>(nullable: false),
                    MPIProcesses = table.Column<int>(nullable: true),
                    OpenMPThreads = table.Column<int>(nullable: true),
                    TaskSpecificationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskParalizationSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskParalizationSpecification_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskSpecificationRequiredNode",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NodeName = table.Column<string>(maxLength: 40, nullable: false),
                    TaskSpecificationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSpecificationRequiredNode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSpecificationRequiredNode_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeRequestedGroup_ClusterNodeTypeId",
                table: "ClusterNodeTypeRequestedGroup",
                column: "ClusterNodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo",
                column: "SubmittedTaskInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskParalizationSpecification_TaskSpecificationId",
                table: "TaskParalizationSpecification",
                column: "TaskSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecificationRequiredNode_TaskSpecificationId",
                table: "TaskSpecificationRequiredNode",
                column: "TaskSpecificationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClusterNodeTypeRequestedGroup");

            migrationBuilder.DropTable(
                name: "SubmittedTaskAllocationNodeInfo");

            migrationBuilder.DropTable(
                name: "TaskParalizationSpecification");

            migrationBuilder.DropTable(
                name: "TaskSpecificationRequiredNode");

            migrationBuilder.DropColumn(
                name: "PlacementPolicy",
                table: "TaskSpecification");

            migrationBuilder.AlterColumn<string>(
                name: "JobArrays",
                table: "TaskSpecification",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredNodes",
                table: "TaskSpecification",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllocatedCoreIds",
                table: "SubmittedTaskInfo",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedNodeGroups",
                table: "ClusterNodeType",
                maxLength: 500,
                nullable: true);
        }
    }
}
