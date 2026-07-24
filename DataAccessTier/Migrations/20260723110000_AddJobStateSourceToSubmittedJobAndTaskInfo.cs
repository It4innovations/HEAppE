using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddJobStateSourceToSubmittedJobAndTaskInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StateSource",
                table: "SubmittedJobInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StateUpdatedAt",
                table: "SubmittedJobInfo",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateSource",
                table: "SubmittedTaskInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StateUpdatedAt",
                table: "SubmittedTaskInfo",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateSource",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "StateUpdatedAt",
                table: "SubmittedJobInfo");

            migrationBuilder.DropColumn(
                name: "StateSource",
                table: "SubmittedTaskInfo");

            migrationBuilder.DropColumn(
                name: "StateUpdatedAt",
                table: "SubmittedTaskInfo");
        }
    }
}
