using CustomerNotificationService.Domain.Entities;
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
        // TODO: configure entity relationships and constraints
        modelBuilder.Entity<Template>().HasIndex(t => t.Key).IsUnique();
    }
}
