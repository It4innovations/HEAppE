using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class ExpandExternalServiceHealthLogForJobTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Protocol",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EndpointOrHost",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<long>(
                name: "ClusterId",
                table: "ExternalServiceHealthLog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JobId",
                table: "ExternalServiceHealthLog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusCode",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaskId",
                table: "ExternalServiceHealthLog",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalServiceHealthLog_ClusterId",
                table: "ExternalServiceHealthLog",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalServiceHealthLog_JobId",
                table: "ExternalServiceHealthLog",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalServiceHealthLog_Timestamp_ServiceName",
                table: "ExternalServiceHealthLog",
                columns: new[] { "Timestamp", "ServiceName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalServiceHealthLog_ClusterId",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropIndex(
                name: "IX_ExternalServiceHealthLog_JobId",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropIndex(
                name: "IX_ExternalServiceHealthLog_Timestamp_ServiceName",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropColumn(
                name: "ClusterId",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "ExternalServiceHealthLog");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "ExternalServiceHealthLog");

            migrationBuilder.AlterColumn<string>(
                name: "Protocol",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EndpointOrHost",
                table: "ExternalServiceHealthLog",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
