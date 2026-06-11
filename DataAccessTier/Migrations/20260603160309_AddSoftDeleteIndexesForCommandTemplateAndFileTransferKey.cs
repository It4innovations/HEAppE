using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteIndexesForCommandTemplateAndFileTransferKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_State_SubmittedJobInfoId",
                table: "SubmittedTaskInfo",
                columns: new[] { "State", "SubmittedJobInfoId" },
                filter: "[State] > 1 AND [State] < 16")
                .Annotation("SqlServer:Include", new[] { "NodeTypeId", "SpecificationId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo",
                columns: new[] { "SubmittedJobInfoId", "State" })
                .Annotation("SqlServer:Include", new[] { "SpecificationId", "NodeTypeId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferTemporaryKey_IsDeleted",
                table: "FileTransferTemporaryKey",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplate_IsDeleted",
                table: "CommandTemplate",
                column: "IsDeleted",
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_State_SubmittedJobInfoId",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropIndex(
                name: "IX_FileTransferTemporaryKey_IsDeleted",
                table: "FileTransferTemporaryKey");

            migrationBuilder.DropIndex(
                name: "IX_CommandTemplate_IsDeleted",
                table: "CommandTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId_State",
                table: "SubmittedTaskInfo",
                columns: new[] { "SubmittedJobInfoId", "State" },
                filter: "[State] > 1 AND [State] < 16")
                .Annotation("SqlServer:Include", new[] { "NodeTypeId", "SpecificationId", "ProjectId" });
        }
    }
}
