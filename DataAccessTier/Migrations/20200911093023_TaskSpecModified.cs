using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class TaskSpecModified : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobSpecification_ClusterNodeType_NodeTypeId",
                table: "JobSpecification");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedJobInfo_ClusterNodeType_NodeTypeId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_NodeTypeId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropIndex(
                name: "IX_JobSpecification_NodeTypeId",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "AllParameters",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "NodeTypeId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "ScheduledJobId",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "MaxCores",
                table: "JobTemplate");

            migrationBuilder.DropColumn(
                name: "MinCores",
                table: "JobTemplate");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "JobTemplate");

            migrationBuilder.DropColumn(
                name: "MaxCores",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "MinCores",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "NodeTypeId",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "JobSpecification");

            migrationBuilder.DropColumn(
                name: "ClusterLocalBasepath",
                table: "ClusterNodeType");

            migrationBuilder.AddColumn<string>(
                name: "LocalBasepath",
                table: "Cluster",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalBasepath",
                table: "Cluster");

            migrationBuilder.AddColumn<string>(
                name: "AllParameters",
                table: "SubmittedJobInfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NodeTypeId",
                table: "SubmittedJobInfo",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "SubmittedJobInfo",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduledJobId",
                table: "SubmittedJobInfo",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxCores",
                table: "JobTemplate",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinCores",
                table: "JobTemplate",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "JobTemplate",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCores",
                table: "JobSpecification",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinCores",
                table: "JobSpecification",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NodeTypeId",
                table: "JobSpecification",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "JobSpecification",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClusterLocalBasepath",
                table: "ClusterNodeType",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_NodeTypeId",
                table: "SubmittedJobInfo",
                column: "NodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_NodeTypeId",
                table: "JobSpecification",
                column: "NodeTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_ClusterNodeType_NodeTypeId",
                table: "JobSpecification",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedJobInfo_ClusterNodeType_NodeTypeId",
                table: "SubmittedJobInfo",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
