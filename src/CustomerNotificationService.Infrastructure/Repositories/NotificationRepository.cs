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
}
