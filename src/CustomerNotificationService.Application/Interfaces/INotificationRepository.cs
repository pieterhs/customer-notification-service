using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;

namespace CustomerNotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification> CreateNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetCustomerHistoryAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IQueryable<Notification>> GetNotificationsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<List<DeliveryAttempt>> GetDeliveryAttemptsByNotificationIdsAsync(List<Guid> notificationIds, CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerNotificationHistoryItemDto>> GetCustomerNotificationHistoryAsync(CustomerNotificationHistoryRequest request, CancellationToken cancellationToken = default);
}
