using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFinishedTaskIndexWithJobId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo",
                column: "State",
                filter: "[State] >= 16")
                .Annotation("SqlServer:Include", new[] { "ProjectId", "SpecificationId", "SubmittedJobInfoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_State",
                table: "SubmittedTaskInfo",
                column: "State",
                filter: "[State] >= 16")
                .Annotation("SqlServer:Include", new[] { "ProjectId", "SpecificationId" });
        }
    }
}
