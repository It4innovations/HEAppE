using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class TaskSpec : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_TaskSpecification_TaskSpecificationId",
                table: "TaskSpecification");

            migrationBuilder.RenameColumn(
                name: "TaskSpecificationId",
                table: "TaskSpecification",
                newName: "NodeTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskSpecification_TaskSpecificationId",
                table: "TaskSpecification",
                newName: "IX_TaskSpecification_NodeTypeId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRerunnable",
                table: "TaskSpecification",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsExclusive",
                table: "TaskSpecification",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CpuHyperThreading",
                table: "TaskSpecification",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobArrays",
                table: "TaskSpecification",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TaskSpecification",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "TaskSpecification",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CpuHyperThreading",
                table: "SubmittedTaskInfo",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NodeTypeId",
                table: "SubmittedTaskInfo",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "SubmittedTaskInfo",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScheduledJobId",
                table: "SubmittedTaskInfo",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaskTemplateId",
                table: "PropertyChangeSpecification",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ClusterId",
                table: "JobSpecification",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileTransferMethodId",
                table: "JobSpecification",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaskTemplateId",
                table: "EnvironmentVariable",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaskTemplateId",
                table: "ClusterNodeType",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskDependency",
                columns: table => new
                {
                    TaskSpecificationId = table.Column<long>(nullable: false),
                    ParentTaskSpecificationId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependency", x => new { x.TaskSpecificationId, x.ParentTaskSpecificationId });
                    table.ForeignKey(
                        name: "FK_TaskDependency_TaskSpecification_ParentTaskSpecificationId",
                        column: x => x.ParentTaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDependency_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    MinCores = table.Column<int>(nullable: true),
                    MaxCores = table.Column<int>(nullable: true),
                    Priority = table.Column<int>(nullable: true),
                    Project = table.Column<string>(maxLength: 50, nullable: true),
                    WalltimeLimit = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplate", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_NodeTypeId",
                table: "SubmittedTaskInfo",
                column: "NodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyChangeSpecification_TaskTemplateId",
                table: "PropertyChangeSpecification",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_ClusterId",
                table: "JobSpecification",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_FileTransferMethodId",
                table: "JobSpecification",
                column: "FileTransferMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_TaskTemplateId",
                table: "EnvironmentVariable",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_TaskTemplateId",
                table: "ClusterNodeType",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependency_ParentTaskSpecificationId",
                table: "TaskDependency",
                column: "ParentTaskSpecificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterNodeType_TaskTemplate_TaskTemplateId",
                table: "ClusterNodeType",
                column: "TaskTemplateId",
                principalTable: "TaskTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnvironmentVariable_TaskTemplate_TaskTemplateId",
                table: "EnvironmentVariable",
                column: "TaskTemplateId",
                principalTable: "TaskTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_Cluster_ClusterId",
                table: "JobSpecification",
                column: "ClusterId",
                principalTable: "Cluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_FileTransferMethod_FileTransferMethodId",
                table: "JobSpecification",
                column: "FileTransferMethodId",
                principalTable: "FileTransferMethod",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyChangeSpecification_TaskTemplate_TaskTemplateId",
                table: "PropertyChangeSpecification",
                column: "TaskTemplateId",
                principalTable: "TaskTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedTaskInfo_ClusterNodeType_NodeTypeId",
                table: "SubmittedTaskInfo",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_ClusterNodeType_NodeTypeId",
                table: "TaskSpecification",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClusterNodeType_TaskTemplate_TaskTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.DropForeignKey(
                name: "FK_EnvironmentVariable_TaskTemplate_TaskTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_Cluster_ClusterId",
                table: "JobSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_FileTransferMethod_FileTransferMethodId",
                table: "JobSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyChangeSpecification_TaskTemplate_TaskTemplateId",
                table: "PropertyChangeSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedTaskInfo_ClusterNodeType_NodeTypeId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_ClusterNodeType_NodeTypeId",
                table: "TaskSpecification");

            migrationBuilder.DropTable(
                name: "TaskDependency");

            migrationBuilder.DropTable(
                name: "TaskTemplate");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_NodeTypeId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_PropertyChangeSpecification_TaskTemplateId",
                table: "PropertyChangeSpecification");

            migrationBuilder.DropIndex(
                name: "IX_JobSpecification_ClusterId",
                table: "JobSpecification");

            migrationBuilder.DropIndex(
                name: "IX_JobSpecification_FileTransferMethodId",
                table: "JobSpecification");

            migrationBuilder.DropIndex(
                name: "IX_EnvironmentVariable_TaskTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeType_TaskTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.DropColumn(
                name: "CpuHyperThreading",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "JobArrays",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "CpuHyperThreading",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "NodeTypeId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "ScheduledJobId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "PropertyChangeSpecification");

            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "FileTransferMethodId",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.RenameColumn(
                name: "NodeTypeId",
                table: "TaskSpecification",
                newName: "TaskSpecificationId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskSpecification_NodeTypeId",
                table: "TaskSpecification",
                newName: "IX_TaskSpecification_TaskSpecificationId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRerunnable",
                table: "TaskSpecification",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<bool>(
                name: "IsExclusive",
                table: "TaskSpecification",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_TaskSpecification_TaskSpecificationId",
                table: "TaskSpecification",
                column: "TaskSpecificationId",
                principalTable: "TaskSpecification",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
