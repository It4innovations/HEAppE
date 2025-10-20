using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class ClusterProjectCredentialsCheckLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClusterProjectCredentialsCheckLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClusterProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ClusterAuthenticationCredentialsId = table.Column<long>(type: "bigint", nullable: false),
                    CheckTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VaultCredentialOk = table.Column<bool>(type: "bit", nullable: true),
                    ClusterConnectionOk = table.Column<bool>(type: "bit", nullable: true),
                    DryRunJobOk = table.Column<bool>(type: "bit", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterProjectCredentialsCheckLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterProjectCredentialsCheckLog_ClusterProjectCredentials_ClusterProjectId_ClusterAuthenticationCredentialsId",
                        columns: x => new { x.ClusterProjectId, x.ClusterAuthenticationCredentialsId },
                        principalTable: "ClusterProjectCredentials",
                        principalColumns: new[] { "ClusterProjectId", "ClusterAuthenticationCredentialsId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentialsCheckLog_ClusterProjectId_ClusterAuthenticationCredentialsId",
                table: "ClusterProjectCredentialsCheckLog",
                columns: new[] { "ClusterProjectId", "ClusterAuthenticationCredentialsId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentialsCheckLog_ClusterProjectId_CheckTimestamp",
                table: "ClusterProjectCredentialsCheckLog",
                columns: new[] { "ClusterProjectId", "CheckTimestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClusterProjectCredentialsCheckLog");
        }
    }
}
