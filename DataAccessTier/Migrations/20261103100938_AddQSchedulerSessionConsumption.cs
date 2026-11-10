using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class AddQSchedulerSessionConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ConsumedMs",
                table: "QSchedulerSession",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsumptionUpdatedAt",
                table: "QSchedulerSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConsumptionFinal",
                table: "QSchedulerSession",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "QSchedulerSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QSchedulerSession_ClusterId_SessionId",
                table: "QSchedulerSession",
                columns: new[] { "ClusterId", "SessionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QSchedulerSession_ClusterId_SessionId",
                table: "QSchedulerSession");

            migrationBuilder.DropColumn(
                name: "ConsumedMs",
                table: "QSchedulerSession");

            migrationBuilder.DropColumn(
                name: "ConsumptionUpdatedAt",
                table: "QSchedulerSession");

            migrationBuilder.DropColumn(
                name: "IsConsumptionFinal",
                table: "QSchedulerSession");

            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "QSchedulerSession");
        }
    }
}
