using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddFilteredIndexesOnAdaptorUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_Email",
                table: "AdaptorUser",
                column: "Email",
                filter: "[IsDeleted] = 0 AND [Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_IdpSid",
                table: "AdaptorUser",
                column: "IdpSid",
                filter: "[IsDeleted] = 0 AND [IdpSid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_Password",
                table: "AdaptorUser",
                column: "Password",
                filter: "[IsDeleted] = 0 AND [Password] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_Username",
                table: "AdaptorUser",
                column: "Username",
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdaptorUser_Email",
                table: "AdaptorUser");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUser_IdpSid",
                table: "AdaptorUser");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUser_Password",
                table: "AdaptorUser");

            migrationBuilder.DropIndex(
                name: "IX_AdaptorUser_Username",
                table: "AdaptorUser");
        }
    }
}
