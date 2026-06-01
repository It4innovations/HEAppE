using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredActiveTasksIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo",
                columns: new[] { "SubmittedJobInfoId", "State" },
                filter: "[State] > 1 AND [State] < 16")
                .Annotation("SqlServer:Include", new[] { "NodeTypeId", "SpecificationId", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo",
                columns: new[] { "SubmittedJobInfoId", "State" })
                .Annotation("SqlServer:Include", new[] { "SpecificationId", "NodeTypeId", "ProjectId" });
        }
    }
}
