using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Postly.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDenormalizedActorFieldsToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayName",
                table: "Notifications",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActorUsername",
                table: "Notifications",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Populate denormalized fields from ActorUser for existing notifications
            migrationBuilder.Sql(@"
                UPDATE Notifications
                SET ActorUsername = (SELECT Username FROM UserAccounts WHERE Id = Notifications.ActorUserId),
                    ActorDisplayName = (SELECT DisplayName FROM UserAccounts WHERE Id = Notifications.ActorUserId)
                WHERE ActorUserId IS NOT NULL
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorDisplayName",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActorUsername",
                table: "Notifications");
        }
    }
}
