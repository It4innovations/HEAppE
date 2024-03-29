﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class ClusterAuthCredAuthenticationType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClusterAllocationName",
                table: "ClusterNodeType",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthenticationType",
                table: "ClusterAuthenticationCredentials",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UpdateJobStateByServiceAccount",
                table: "Cluster",
                type: "bit",
                nullable: true,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClusterAllocationName",
                table: "ClusterNodeType");

            migrationBuilder.DropColumn(
                name: "AuthenticationType",
                table: "ClusterAuthenticationCredentials");

            migrationBuilder.DropColumn(
                name: "UpdateJobStateByServiceAccount",
                table: "Cluster");
        }
    }
}
