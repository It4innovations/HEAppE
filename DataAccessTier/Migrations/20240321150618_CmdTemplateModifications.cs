using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class CmdTemplateModifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "UseAccountingStringForScheduler",
                table: "Project",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CommandTemplateParameter",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "CommandTemplateParameter",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "CommandTemplateParameter",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CommandTemplate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedFromId",
                table: "CommandTemplate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "CommandTemplate",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplate_CreatedFromId",
                table: "CommandTemplate",
                column: "CreatedFromId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandTemplate_CommandTemplate_CreatedFromId",
                table: "CommandTemplate",
                column: "CreatedFromId",
                principalTable: "CommandTemplate",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandTemplate_CommandTemplate_CreatedFromId",
                table: "CommandTemplate");

            migrationBuilder.DropIndex(
                name: "IX_CommandTemplate_CreatedFromId",
                table: "CommandTemplate");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CommandTemplateParameter");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "CommandTemplateParameter");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "CommandTemplateParameter");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CommandTemplate");

            migrationBuilder.DropColumn(
                name: "CreatedFromId",
                table: "CommandTemplate");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "CommandTemplate");

            migrationBuilder.AlterColumn<bool>(
                name: "UseAccountingStringForScheduler",
                table: "Project",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
