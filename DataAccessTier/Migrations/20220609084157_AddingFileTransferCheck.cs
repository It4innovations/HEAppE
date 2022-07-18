using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class AddingFileTransferCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "AdaptorUser",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "FileTransferTemporaryKey",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicKey = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedJobId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTransferTemporaryKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileTransferTemporaryKey_SubmittedJobInfo_SubmittedJobId",
                        column: x => x.SubmittedJobId,
                        principalTable: "SubmittedJobInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferTemporaryKey_SubmittedJobId",
                table: "FileTransferTemporaryKey",
                column: "SubmittedJobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileTransferTemporaryKey");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "AdaptorUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
