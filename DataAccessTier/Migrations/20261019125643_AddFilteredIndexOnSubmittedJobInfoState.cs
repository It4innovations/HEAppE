using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredIndexOnSubmittedJobInfoState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_State",
                table: "SubmittedJobInfo",
                column: "State",
                filter: "[State] > 1 AND [State] < 16")
                .Annotation("SqlServer:Include", new[] { "SubmitterId", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedJobInfo_State",
                table: "SubmittedJobInfo");
        }
    }
}
