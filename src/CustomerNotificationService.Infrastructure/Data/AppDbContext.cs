using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationQueueItem> NotificationQueue => Set<NotificationQueueItem>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Template configuration
        modelBuilder.Entity<Template>()
            .HasIndex(t => t.Key)
            .IsUnique();

        // Notification configuration
        modelBuilder.Entity<Notification>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(e => e.Channel)
            .HasConversion<string>();

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
