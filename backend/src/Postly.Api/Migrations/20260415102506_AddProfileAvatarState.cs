using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Postly.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileAvatarState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AvatarBytes",
                table: "UserAccounts",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "UserAccounts",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvatarUpdatedAtUtc",
                table: "UserAccounts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarBytes",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "AvatarUpdatedAtUtc",
                table: "UserAccounts");
        }
    }
}
