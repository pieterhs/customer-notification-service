using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;
    public NotificationRepository(AppDbContext db) => _db = db;

    public async Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _db.Notifications.AddAsync(notification, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        notification.Id = Guid.NewGuid();
        notification.CreatedAt = DateTimeOffset.UtcNow;
        await _db.Notifications.AddAsync(notification, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return notification;
    }
    
    public async Task<List<Notification>> GetCustomerHistoryAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<IQueryable<Notification>> GetNotificationsByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // To satisfy async signature while returning IQueryable
        return _db.Notifications
            .AsNoTracking()
            .Where(n => n.CustomerId == customerId);
    }

    public async Task<List<DeliveryAttempt>> GetDeliveryAttemptsByNotificationIdsAsync(List<Guid> notificationIds, CancellationToken cancellationToken = default)
    {
        return await _db.DeliveryAttempts
            .AsNoTracking()
            .Where(a => notificationIds.Contains(a.NotificationId))
            .ToListAsync(cancellationToken);
    }
}
