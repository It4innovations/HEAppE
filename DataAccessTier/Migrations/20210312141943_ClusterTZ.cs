using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class ClusterTZ : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_Cluster_ClusterId",
                table: "JobSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfo_SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_ClusterNodeType_NodeTypeId",
                table: "TaskSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification");

            migrationBuilder.DropIndex(
                name: "IX_TaskSpecification_NodeTypeId",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "NodeTypeId",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "SharedBasepath",
                table: "FileTransferMethod");

            migrationBuilder.AlterColumn<long>(
                name: "CommandTemplateId",
                table: "TaskSpecification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ClusterNodeTypeId",
                table: "TaskSpecification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ClusterId",
                table: "JobSpecification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ClusterId",
                table: "FileTransferMethod",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Cluster",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_ClusterNodeTypeId",
                table: "TaskSpecification",
                column: "ClusterNodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferMethod_ClusterId",
                table: "FileTransferMethod",
                column: "ClusterId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileTransferMethod_Cluster_ClusterId",
                table: "FileTransferMethod",
                column: "ClusterId",
                principalTable: "Cluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_Cluster_ClusterId",
                table: "JobSpecification",
                column: "ClusterId",
                principalTable: "Cluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfo_SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo",
                column: "SubmittedTaskInfoId",
                principalTable: "SubmittedTaskInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_ClusterNodeType_ClusterNodeTypeId",
                table: "TaskSpecification",
                column: "ClusterNodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification",
                column: "CommandTemplateId",
                principalTable: "CommandTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileTransferMethod_Cluster_ClusterId",
                table: "FileTransferMethod");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_Cluster_ClusterId",
                table: "JobSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfo_SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_ClusterNodeType_ClusterNodeTypeId",
                table: "TaskSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification");

            migrationBuilder.DropIndex(
                name: "IX_TaskSpecification_ClusterNodeTypeId",
                table: "TaskSpecification");

            migrationBuilder.DropIndex(
                name: "IX_FileTransferMethod_ClusterId",
                table: "FileTransferMethod");

            migrationBuilder.DropColumn(
                name: "ClusterNodeTypeId",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "FileTransferMethod");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Cluster");

            migrationBuilder.AlterColumn<long>(
                name: "CommandTemplateId",
                table: "TaskSpecification",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "NodeTypeId",
                table: "TaskSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "ClusterId",
                table: "JobSpecification",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "SharedBasepath",
                table: "FileTransferMethod",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_NodeTypeId",
                table: "TaskSpecification",
                column: "NodeTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_Cluster_ClusterId",
                table: "JobSpecification",
                column: "ClusterId",
                principalTable: "Cluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfo_SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo",
                column: "SubmittedTaskInfoId",
                principalTable: "SubmittedTaskInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_ClusterNodeType_NodeTypeId",
                table: "TaskSpecification",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification",
                column: "CommandTemplateId",
                principalTable: "CommandTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
