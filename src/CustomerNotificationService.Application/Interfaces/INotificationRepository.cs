using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetCustomerHistoryAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IQueryable<Notification>> GetNotificationsByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<List<DeliveryAttempt>> GetDeliveryAttemptsByNotificationIdsAsync(List<Guid> notificationIds, CancellationToken cancellationToken = default);
}
