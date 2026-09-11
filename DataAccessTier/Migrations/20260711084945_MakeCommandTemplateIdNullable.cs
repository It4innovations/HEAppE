using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class MakeCommandTemplateIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification");

            migrationBuilder.AlterColumn<long>(
                name: "CommandTemplateId",
                table: "TaskSpecification",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification",
                column: "CommandTemplateId",
                principalTable: "CommandTemplate",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification");

            migrationBuilder.AlterColumn<long>(
                name: "CommandTemplateId",
                table: "TaskSpecification",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                table: "TaskSpecification",
                column: "CommandTemplateId",
                principalTable: "CommandTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
