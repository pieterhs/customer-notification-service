using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerNotificationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PerformedBy",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "PerformedAt",
                table: "AuditLogs",
                newName: "Timestamp");

            migrationBuilder.AddColumn<Guid>(
                name: "NotificationId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationId",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "AuditLogs",
                newName: "PerformedAt");

            migrationBuilder.AddColumn<string>(
                name: "PerformedBy",
                table: "AuditLogs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
