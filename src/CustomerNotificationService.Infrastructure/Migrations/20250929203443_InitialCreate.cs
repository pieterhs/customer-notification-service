using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerNotificationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PerformedBy = table.Column<string>(type: "text", nullable: false),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseMessage = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnqueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    JobStatus = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Recipient = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    TemplateKey = table.Column<string>(type: "text", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SendAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomerId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_ReadyAt_JobStatus",
                table: "NotificationQueue",
                columns: new[] { "ReadyAt", "JobStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Key",
                table: "Templates",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DeliveryAttempts");

            migrationBuilder.DropTable(
                name: "NotificationQueue");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Templates");
        }
    }
}
