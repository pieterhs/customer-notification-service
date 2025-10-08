using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationQueueItem> NotificationQueue => Set<NotificationQueueItem>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Legacy Template entity removed

        // NotificationTemplate configuration
        modelBuilder.Entity<NotificationTemplate>()
            .ToTable("NotificationTemplates");
        modelBuilder.Entity<NotificationTemplate>()
            .HasIndex(t => new { t.Name, t.Channel });

        // Notification configuration
        modelBuilder.Entity<Notification>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(e => e.Channel)
            .HasConversion<string>();

        // Notification indexes for optimal history query performance
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.CustomerId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_CustomerId_CreatedAt")
            .IsDescending(false, true); // CustomerId ASC, CreatedAt DESC

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.CustomerId, n.Status, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_CustomerId_Status_CreatedAt")
            .IsDescending(false, false, true); // CustomerId ASC, Status ASC, CreatedAt DESC

        // Idempotency key unique index (filtered for non-null values)
        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.IdempotencyKey)
            .HasDatabaseName("IX_Notifications_IdempotencyKey")
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL"); // PostgreSQL syntax

        // Queue item configuration
        modelBuilder.Entity<NotificationQueueItem>()
            .ToTable("NotificationQueue");
        modelBuilder.Entity<NotificationQueueItem>()
            .HasIndex(q => new { q.ReadyAt, q.JobStatus });
        modelBuilder.Entity<NotificationQueueItem>()
            .HasIndex(q => new { q.NextAttemptAt, q.JobStatus });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>()
            .ToTable("AuditLogs");
    }
}
