using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Postly.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReplySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Posts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReplyToPostId",
                table: "Posts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ReplyToPostId_CreatedAtUtc_Id",
                table: "Posts",
                columns: new[] { "ReplyToPostId", "CreatedAtUtc", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_ReplyToPostId_CreatedAtUtc_Id",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ReplyToPostId",
                table: "Posts");
        }
    }
}
