using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class Vault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "ClusterAuthenticationCredentials");

            migrationBuilder.DropColumn(
                name: "PrivateKeyFile",
                table: "ClusterAuthenticationCredentials");

            migrationBuilder.DropColumn(
                name: "PrivateKeyPassword",
                table: "ClusterAuthenticationCredentials");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "ClusterAuthenticationCredentials",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivateKeyFile",
                table: "ClusterAuthenticationCredentials",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivateKeyPassword",
                table: "ClusterAuthenticationCredentials",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
