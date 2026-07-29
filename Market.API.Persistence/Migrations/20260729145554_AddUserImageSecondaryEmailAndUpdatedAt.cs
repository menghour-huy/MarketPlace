using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Market.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserImageSecondaryEmailAndUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "core",
                table: "tbl_Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryEmail",
                schema: "core",
                table: "tbl_Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "core",
                table: "tbl_Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "core",
                table: "tbl_Users");

            migrationBuilder.DropColumn(
                name: "SecondaryEmail",
                schema: "core",
                table: "tbl_Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "core",
                table: "tbl_Users");
        }
    }
}
