using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class MultiProjectSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cluster_ClusterAuthenticationCredentials_ServiceAccountCredentialsId",
                table: "Cluster");

            migrationBuilder.DropForeignKey(
                name: "FK_ClusterAuthenticationCredentials_Cluster_ClusterId",
                table: "ClusterAuthenticationCredentials");

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
                name: "AdaptorUserUserGroup");

            migrationBuilder.DropTable(
                name: "AdaptorUserUserRole");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentialProjectDomain");

            migrationBuilder.DropTable(
                name: "PropertyChangeSpecification");

            migrationBuilder.DropTable(
                name: "OpenStackProjectDomain");

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

            migrationBuilder.DropIndex(
                name: "IX_ClusterAuthenticationCredentials_ClusterId",
                table: "ClusterAuthenticationCredentials");

            migrationBuilder.DropIndex(
                name: "IX_Cluster_ServiceAccountCredentialsId",
                table: "Cluster");

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

            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "ClusterAuthenticationCredentials");

            migrationBuilder.DropColumn(
                name: "LocalBasepath",
                table: "Cluster");

            migrationBuilder.DropColumn(
                name: "ServiceAccountCredentialsId",
                table: "Cluster");

            migrationBuilder.DropColumn(
                name: "AccountingString",
                table: "AdaptorUserGroup");

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
                name: "AdaptorUserGroupId",
                table: "OpenStackProject",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "UID",
                table: "OpenStackDomain",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "JobSpecification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "CommandTemplate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MasterNodeName",
                table: "Cluster",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "DomainName",
                table: "Cluster",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProjectId",
                table: "AdaptorUserGroup",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdaptorUserUserGroupRole",
                columns: table => new
                {
                    AdaptorUserId = table.Column<long>(type: "bigint", nullable: false),
                    AdaptorUserGroupId = table.Column<long>(type: "bigint", nullable: false),
                    AdaptorUserRoleId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserUserGroupRole", x => new { x.AdaptorUserId, x.AdaptorUserGroupId, x.AdaptorUserRoleId });
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroupRole_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroupRole_AdaptorUserGroup_AdaptorUserGroupId",
                        column: x => x.AdaptorUserGroupId,
                        principalTable: "AdaptorUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroupRole_AdaptorUserRole_AdaptorUserRoleId",
                        column: x => x.AdaptorUserRoleId,
                        principalTable: "AdaptorUserRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredentialProject",
                columns: table => new
                {
                    OpenStackAuthenticationCredentialId = table.Column<long>(type: "bigint", nullable: false),
                    OpenStackProjectId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredentialProject", x => new { x.OpenStackAuthenticationCredentialId, x.OpenStackProjectId });
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialProject_OpenStackAuthenticationCredential_OpenStackAuthenticationCredentialId",
                        column: x => x.OpenStackAuthenticationCredentialId,
                        principalTable: "OpenStackAuthenticationCredential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialProject_OpenStackProject_OpenStackProjectId",
                        column: x => x.OpenStackProjectId,
                        principalTable: "OpenStackProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountingString = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClusterProject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClusterId = table.Column<long>(type: "bigint", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    LocalBasepath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterProject_Cluster_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Cluster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClusterProject_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClusterProjectCredentials",
                columns: table => new
                {
                    ClusterProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ClusterAuthenticationCredentialsId = table.Column<long>(type: "bigint", nullable: false),
                    IsServiceAccount = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterProjectCredentials", x => new { x.ClusterProjectId, x.ClusterAuthenticationCredentialsId });
                    table.ForeignKey(
                        name: "FK_ClusterProjectCredentials_ClusterAuthenticationCredentials_ClusterAuthenticationCredentialsId",
                        column: x => x.ClusterAuthenticationCredentialsId,
                        principalTable: "ClusterAuthenticationCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClusterProjectCredentials_ClusterProject_ClusterProjectId",
                        column: x => x.ClusterProjectId,
                        principalTable: "ClusterProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_OpenStackProject_AdaptorUserGroupId",
                table: "OpenStackProject",
                column: "AdaptorUserGroupId");

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

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroupRole_AdaptorUserGroupId",
                table: "AdaptorUserUserGroupRole",
                column: "AdaptorUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroupRole_AdaptorUserRoleId",
                table: "AdaptorUserUserGroupRole",
                column: "AdaptorUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProject_ClusterId_ProjectId",
                table: "ClusterProject",
                columns: new[] { "ClusterId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProject_ProjectId",
                table: "ClusterProject",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentials_ClusterAuthenticationCredentialsId",
                table: "ClusterProjectCredentials",
                column: "ClusterAuthenticationCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentialProject_OpenStackProjectId",
                table: "OpenStackAuthenticationCredentialProject",
                column: "OpenStackProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_AccountingString",
                table: "Project",
                column: "AccountingString",
                unique: true);

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
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OpenStackProject_AdaptorUserGroup_AdaptorUserGroupId",
                table: "OpenStackProject",
                column: "AdaptorUserGroupId",
                principalTable: "AdaptorUserGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_OpenStackProject_AdaptorUserGroup_AdaptorUserGroupId",
                table: "OpenStackProject");

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
                name: "AdaptorUserUserGroupRole");

            migrationBuilder.DropTable(
                name: "ClusterProjectCredentials");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentialProject");

            migrationBuilder.DropTable(
                name: "ClusterProject");

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
                name: "IX_OpenStackProject_AdaptorUserGroupId",
                table: "OpenStackProject");

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
                name: "AdaptorUserGroupId",
                table: "OpenStackProject");

            migrationBuilder.DropColumn(
                name: "UID",
                table: "OpenStackDomain");

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

            migrationBuilder.AddColumn<long>(
                name: "ClusterId",
                table: "ClusterAuthenticationCredentials",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MasterNodeName",
                table: "Cluster",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DomainName",
                table: "Cluster",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalBasepath",
                table: "Cluster",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ServiceAccountCredentialsId",
                table: "Cluster",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountingString",
                table: "AdaptorUserGroup",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdaptorUserUserGroup",
                columns: table => new
                {
                    AdaptorUserId = table.Column<long>(type: "bigint", nullable: false),
                    AdaptorUserGroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserUserGroup", x => new { x.AdaptorUserId, x.AdaptorUserGroupId });
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroup_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroup_AdaptorUserGroup_AdaptorUserGroupId",
                        column: x => x.AdaptorUserGroupId,
                        principalTable: "AdaptorUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUserUserRole",
                columns: table => new
                {
                    AdaptorUserId = table.Column<long>(type: "bigint", nullable: false),
                    AdaptorUserRoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserUserRole", x => new { x.AdaptorUserId, x.AdaptorUserRoleId });
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserRole_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserRole_AdaptorUserRole_AdaptorUserRoleId",
                        column: x => x.AdaptorUserRoleId,
                        principalTable: "AdaptorUserRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "OpenStackProjectDomain",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OpenStackProjectId = table.Column<long>(type: "bigint", nullable: false),
                    UID = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
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
                name: "IX_ClusterAuthenticationCredentials_ClusterId",
                table: "ClusterAuthenticationCredentials",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Cluster_ServiceAccountCredentialsId",
                table: "Cluster",
                column: "ServiceAccountCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroup_AdaptorUserGroupId",
                table: "AdaptorUserUserGroup",
                column: "AdaptorUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserRole_AdaptorUserRoleId",
                table: "AdaptorUserUserRole",
                column: "AdaptorUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentialProjectDomain_OpenStackProjectDomainId",
                table: "OpenStackAuthenticationCredentialProjectDomain",
                column: "OpenStackProjectDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackProjectDomain_OpenStackProjectId",
                table: "OpenStackProjectDomain",
                column: "OpenStackProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyChangeSpecification_JobTemplateId",
                table: "PropertyChangeSpecification",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyChangeSpecification_TaskTemplateId",
                table: "PropertyChangeSpecification",
                column: "TaskTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cluster_ClusterAuthenticationCredentials_ServiceAccountCredentialsId",
                table: "Cluster",
                column: "ServiceAccountCredentialsId",
                principalTable: "ClusterAuthenticationCredentials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterAuthenticationCredentials_Cluster_ClusterId",
                table: "ClusterAuthenticationCredentials",
                column: "ClusterId",
                principalTable: "Cluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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
