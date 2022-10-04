using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class MultiProjectSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClusterNodeType_JobTemplate_JobTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.DropForeignKey(
                name: "FK_ClusterNodeType_TaskTemplate_TaskTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.DropForeignKey(
                name: "FK_EnvironmentVariable_JobTemplate_JobTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropForeignKey(
                name: "FK_EnvironmentVariable_TaskTemplate_TaskTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropTable(
                name: "PropertyChangeSpecification");

            migrationBuilder.DropTable(
                name: "JobTemplate");

            migrationBuilder.DropTable(
                name: "TaskTemplate");

            migrationBuilder.DropIndex(
                name: "IX_EnvironmentVariable_JobTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropIndex(
                name: "IX_EnvironmentVariable_TaskTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeType_JobTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeType_TaskTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "JobTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "EnvironmentVariable");

            migrationBuilder.DropColumn(
                name: "JobTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "ClusterNodeType");

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "TaskSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "SubmittedTaskInfo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "SubmittedJobInfo",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "JobSpecification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "CommandTemplate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "AdaptorUserGroup",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountingString = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_ProjectId",
                table: "TaskSpecification",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_ProjectId",
                table: "SubmittedTaskInfo",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_ProjectId",
                table: "SubmittedJobInfo",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_ProjectId",
                table: "JobSpecification",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplate_ProjectId",
                table: "CommandTemplate",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserGroup_ProjectId",
                table: "AdaptorUserGroup",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdaptorUserGroup_Project_ProjectId",
                table: "AdaptorUserGroup",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandTemplate_Project_ProjectId",
                table: "CommandTemplate",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_Project_ProjectId",
                table: "JobSpecification",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedJobInfo_Project_ProjectId",
                table: "SubmittedJobInfo",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedTaskInfo_Project_ProjectId",
                table: "SubmittedTaskInfo",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_Project_ProjectId",
                table: "TaskSpecification",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdaptorUserGroup_Project_ProjectId",
                table: "AdaptorUserGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandTemplate_Project_ProjectId",
                table: "CommandTemplate");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_Project_ProjectId",
                table: "JobSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedJobInfo_Project_ProjectId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedTaskInfo_Project_ProjectId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_Project_ProjectId",
                table: "TaskSpecification");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropIndex(
                name: "IX_TaskSpecification_ProjectId",
                table: "TaskSpecification");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_ProjectId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_ProjectId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_JobSpecification_ProjectId",
                table: "JobSpecification");

            migrationBuilder.DropIndex(
                name: "IX_CommandTemplate_ProjectId",
                table: "CommandTemplate");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUserGroup_ProjectId",
                table: "AdaptorUserGroup");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TaskSpecification");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "CommandTemplate");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "AdaptorUserGroup");

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "TaskSpecification",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "SubmittedJobInfo",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "JobSpecification",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JobTemplateId",
                table: "EnvironmentVariable",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaskTemplateId",
                table: "EnvironmentVariable",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JobTemplateId",
                table: "ClusterNodeType",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaskTemplateId",
                table: "ClusterNodeType",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Project = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WalltimeLimit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaxCores = table.Column<int>(type: "int", nullable: true),
                    MinCores = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    Project = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WalltimeLimit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyChangeSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeMethod = table.Column<int>(type: "int", nullable: false),
                    JobTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    PropertyName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaskTemplateId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyChangeSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyChangeSpecification_JobTemplate_JobTemplateId",
                        column: x => x.JobTemplateId,
                        principalTable: "JobTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyChangeSpecification_TaskTemplate_TaskTemplateId",
                        column: x => x.TaskTemplateId,
                        principalTable: "TaskTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_JobTemplateId",
                table: "EnvironmentVariable",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_TaskTemplateId",
                table: "EnvironmentVariable",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_JobTemplateId",
                table: "ClusterNodeType",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_TaskTemplateId",
                table: "ClusterNodeType",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyChangeSpecification_JobTemplateId",
                table: "PropertyChangeSpecification",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyChangeSpecification_TaskTemplateId",
                table: "PropertyChangeSpecification",
                column: "TaskTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterNodeType_JobTemplate_JobTemplateId",
                table: "ClusterNodeType",
                column: "JobTemplateId",
                principalTable: "JobTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterNodeType_TaskTemplate_TaskTemplateId",
                table: "ClusterNodeType",
                column: "TaskTemplateId",
                principalTable: "TaskTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnvironmentVariable_JobTemplate_JobTemplateId",
                table: "EnvironmentVariable",
                column: "JobTemplateId",
                principalTable: "JobTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EnvironmentVariable_TaskTemplate_TaskTemplateId",
                table: "EnvironmentVariable",
                column: "TaskTemplateId",
                principalTable: "TaskTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
