using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class IsDeletedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubProject_IsDeleted",
                table: "SubProject",
                column: "IsDeleted");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubProject_IsDeleted",
                table: "SubProject");

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
        }
    }
}
