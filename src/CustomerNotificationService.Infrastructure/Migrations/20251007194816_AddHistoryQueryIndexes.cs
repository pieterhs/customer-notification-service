using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerNotificationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CustomerId_CreatedAt",
                table: "Notifications",
                columns: new[] { "CustomerId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CustomerId_Status_CreatedAt",
                table: "Notifications",
                columns: new[] { "CustomerId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_CustomerId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_CustomerId_Status_CreatedAt",
                table: "Notifications");
        }
    }
}
