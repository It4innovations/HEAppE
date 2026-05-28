using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class SubmittedJobInfoIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProject_ProjectId",
                table: "ClusterProject");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo",
                column: "State",
                filter: "[State] >= 16")
                .Annotation("SqlServer:Include", new[] { "ProjectId", "SpecificationId", "SubmittedJobInfoId", "NodeTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId",
                table: "SubmittedTaskInfo",
                column: "SubmittedJobInfoId")
                .Annotation("SqlServer:Include", new[] { "State", "SpecificationId", "NodeTypeId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentials_ClusterProjectId",
                table: "ClusterProjectCredentials",
                column: "ClusterProjectId",
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "ClusterAuthenticationCredentialsId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProject_ClusterId",
                table: "ClusterProject",
                column: "ClusterId",
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "ProjectId", "PreferredAuthType" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProject_ProjectId",
                table: "ClusterProject",
                column: "ProjectId",
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "ClusterId", "PreferredAuthType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProjectCredentials_ClusterProjectId",
                table: "ClusterProjectCredentials");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProject_ClusterId",
                table: "ClusterProject");

            migrationBuilder.DropIndex(
                name: "IX_ClusterProject_ProjectId",
                table: "ClusterProject");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo",
                column: "State",
                filter: "[State] >= 16")
                .Annotation("SqlServer:Include", new[] { "ProjectId", "SpecificationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProject_ProjectId",
                table: "ClusterProject",
                column: "ProjectId");
        }
    }
}
