using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Postly.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeNotificationActorOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_UserAccounts_ActorUserId",
                table: "Notifications");

            migrationBuilder.AlterColumn<long>(
                name: "ActorUserId",
                table: "Notifications",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_UserAccounts_ActorUserId",
                table: "Notifications",
                column: "ActorUserId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_UserAccounts_ActorUserId",
                table: "Notifications");

            migrationBuilder.AlterColumn<long>(
                name: "ActorUserId",
                table: "Notifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_UserAccounts_ActorUserId",
                table: "Notifications",
                column: "ActorUserId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
