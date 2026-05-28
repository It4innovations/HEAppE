using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskSpecification_JobSpecificationId",
                table: "TaskSpecification");

            migrationBuilder.DropIndex(
                name: "IX_SubProject_IsDeleted",
                table: "SubProject");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_ProjectId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_SpecificationId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_SubmitterId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_ProjectClusterNodeTypeAggregation_IsDeleted",
                table: "ProjectClusterNodeTypeAggregation");

            migrationBuilder.DropIndex(
                name: "IX_Project_IsDeleted",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_FileTransferMethod_IsDeleted",
                table: "FileTransferMethod");

            migrationBuilder.DropIndex(
                name: "IX_Contact_IsDeleted",
                table: "Contact");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProxyConnection_IsDeleted",
                table: "ClusterProxyConnection");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProjectCredentials_IsDeleted",
                table: "ClusterProjectCredentials");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_IsDeleted",
                table: "ClusterNodeTypeAggregationAccounting");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeTypeAggregation_IsDeleted",
                table: "ClusterNodeTypeAggregation");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeType_IsDeleted",
                table: "ClusterNodeType");

            migrationBuilder.DropIndex(
                name: "IX_ClusterAuthenticationCredentials_IsDeleted",
                table: "ClusterAuthenticationCredentials");

            migrationBuilder.DropIndex(
                name: "IX_Cluster_IsDeleted",
                table: "Cluster");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUserUserGroupRole_IsDeleted",
                table: "AdaptorUserUserGroupRole");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUser_IsDeleted",
                table: "AdaptorUser");

            migrationBuilder.DropIndex(
                name: "IX_Accounting_IsDeleted",
                table: "Accounting");

            migrationBuilder.AlterColumn<string>(
                name: "ScheduledJobId",
                table: "SubmittedTaskInfo",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_JobSpecificationId",
                table: "TaskSpecification",
                column: "JobSpecificationId")
                .Annotation("SqlServer:Include", new[] { "CommandTemplateId", "ClusterNodeTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_IsDeleted",
                table: "SubProject",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_ScheduledJobId",
                table: "SubmittedTaskInfo",
                column: "ScheduledJobId",
                filter: "[ScheduledJobId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo",
                column: "State",
                filter: "[State] >= 16")
                .Annotation("SqlServer:Include", new[] { "ProjectId", "SpecificationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo",
                columns: new[] { "SubmittedJobInfoId", "State" })
                .Annotation("SqlServer:Include", new[] { "SpecificationId", "NodeTypeId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_ProjectId_StartTime_EndTime",
                table: "SubmittedJobInfo",
                columns: new[] { "ProjectId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SpecificationId_ProjectId",
                table: "SubmittedJobInfo",
                columns: new[] { "SpecificationId", "ProjectId" })
                .Annotation("SqlServer:Include", new[] { "State", "SubmitterId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SubmitterId",
                table: "SubmittedJobInfo",
                column: "SubmitterId")
                .Annotation("SqlServer:Include", new[] { "State" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClusterNodeTypeAggregation_IsDeleted",
                table: "ProjectClusterNodeTypeAggregation",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Project_IsDeleted",
                table: "Project",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferMethod_IsDeleted",
                table: "FileTransferMethod",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_IsDeleted",
                table: "Contact",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProxyConnection_IsDeleted",
                table: "ClusterProxyConnection",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentialsCheckLog_ClusterAuthenticationCredentialsId",
                table: "ClusterProjectCredentialsCheckLog",
                column: "ClusterAuthenticationCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentials_IsDeleted",
                table: "ClusterProjectCredentials",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_ClusterNodeTypeAggregationId",
                table: "ClusterNodeTypeAggregationAccounting",
                column: "ClusterNodeTypeAggregationId",
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "AccountingId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_IsDeleted",
                table: "ClusterNodeTypeAggregationAccounting",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeAggregation_IsDeleted",
                table: "ClusterNodeTypeAggregation",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_IsDeleted",
                table: "ClusterNodeType",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterAuthenticationCredentials_IsDeleted",
                table: "ClusterAuthenticationCredentials",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Cluster_IsDeleted",
                table: "Cluster",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroupRole_IsDeleted",
                table: "AdaptorUserUserGroupRole",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_IsDeleted",
                table: "AdaptorUser",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Accounting_IsDeleted",
                table: "Accounting",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskSpecification_JobSpecificationId",
                table: "TaskSpecification");

            migrationBuilder.DropIndex(
                name: "IX_SubProject_IsDeleted",
                table: "SubProject");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_ScheduledJobId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_ProjectId_StartTime_EndTime",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_SpecificationId_ProjectId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_SubmitterId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_ProjectClusterNodeTypeAggregation_IsDeleted",
                table: "ProjectClusterNodeTypeAggregation");

            migrationBuilder.DropIndex(
                name: "IX_Project_IsDeleted",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_FileTransferMethod_IsDeleted",
                table: "FileTransferMethod");

            migrationBuilder.DropIndex(
                name: "IX_Contact_IsDeleted",
                table: "Contact");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProxyConnection_IsDeleted",
                table: "ClusterProxyConnection");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProjectCredentialsCheckLog_ClusterAuthenticationCredentialsId",
                table: "ClusterProjectCredentialsCheckLog");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProjectCredentials_IsDeleted",
                table: "ClusterProjectCredentials");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_ClusterNodeTypeAggregationId",
                table: "ClusterNodeTypeAggregationAccounting");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_IsDeleted",
                table: "ClusterNodeTypeAggregationAccounting");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeTypeAggregation_IsDeleted",
                table: "ClusterNodeTypeAggregation");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodeType_IsDeleted",
                table: "ClusterNodeType");

            migrationBuilder.DropIndex(
                name: "IX_ClusterAuthenticationCredentials_IsDeleted",
                table: "ClusterAuthenticationCredentials");

            migrationBuilder.DropIndex(
                name: "IX_Cluster_IsDeleted",
                table: "Cluster");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUserUserGroupRole_IsDeleted",
                table: "AdaptorUserUserGroupRole");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUser_IsDeleted",
                table: "AdaptorUser");

            migrationBuilder.DropIndex(
                name: "IX_Accounting_IsDeleted",
                table: "Accounting");

            migrationBuilder.AlterColumn<string>(
                name: "ScheduledJobId",
                table: "SubmittedTaskInfo",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_JobSpecificationId",
                table: "TaskSpecification",
                column: "JobSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_IsDeleted",
                table: "SubProject",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId",
                table: "SubmittedTaskInfo",
                column: "SubmittedJobInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_ProjectId",
                table: "SubmittedJobInfo",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SpecificationId",
                table: "SubmittedJobInfo",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SubmitterId",
                table: "SubmittedJobInfo",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClusterNodeTypeAggregation_IsDeleted",
                table: "ProjectClusterNodeTypeAggregation",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Project_IsDeleted",
                table: "Project",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferMethod_IsDeleted",
                table: "FileTransferMethod",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_IsDeleted",
                table: "Contact",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProxyConnection_IsDeleted",
                table: "ClusterProxyConnection",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentials_IsDeleted",
                table: "ClusterProjectCredentials",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_IsDeleted",
                table: "ClusterNodeTypeAggregationAccounting",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeAggregation_IsDeleted",
                table: "ClusterNodeTypeAggregation",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_IsDeleted",
                table: "ClusterNodeType",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterAuthenticationCredentials_IsDeleted",
                table: "ClusterAuthenticationCredentials",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Cluster_IsDeleted",
                table: "Cluster",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroupRole_IsDeleted",
                table: "AdaptorUserUserGroupRole",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_IsDeleted",
                table: "AdaptorUser",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Accounting_IsDeleted",
                table: "Accounting",
                column: "IsDeleted");
        }
    }
}
