using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;
    public NotificationRepository(AppDbContext db) => _db = db;

    public async Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<Notification?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => await _db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.IdempotencyKey == idempotencyKey, cancellationToken);

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

    public async Task<IQueryable<Notification>> GetNotificationsByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // To satisfy async signature while returning IQueryable
        return _db.Notifications
            .AsNoTracking()
            .Where(n => n.CustomerId == customerId.ToString());
    }

    public async Task<List<DeliveryAttempt>> GetDeliveryAttemptsByNotificationIdsAsync(List<Guid> notificationIds, CancellationToken cancellationToken = default)
    {
        return await _db.DeliveryAttempts
            .AsNoTracking()
            .Where(a => notificationIds.Contains(a.NotificationId))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<CustomerNotificationHistoryItemDto>> GetCustomerNotificationHistoryAsync(CustomerNotificationHistoryRequest request, CancellationToken cancellationToken = default)
    {
        // Build the base query with customer filter
        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.CustomerId == request.CustomerId.ToString());

        // Apply status filter if provided
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<NotificationStatus>(request.Status, true, out var statusEnum))
            {
                query = query.Where(n => n.Status == statusEnum);
            }
            else
            {
                throw new ArgumentException($"Invalid status value: {request.Status}");
            }
        }

        // Apply date range filters
        if (request.From.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(n => n.CreatedAt <= request.To.Value);
        }

        // Get total count for pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply ordering and pagination
        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get delivery attempts for the notifications in this page
        var notificationIds = notifications.Select(n => n.Id).ToList();
        var attempts = await _db.DeliveryAttempts
            .AsNoTracking()
            .Where(a => notificationIds.Contains(a.NotificationId))
            .ToListAsync(cancellationToken);

        // Project to DTOs with computed fields
        var items = notifications.Select(n =>
        {
            var notificationAttempts = attempts.Where(a => a.NotificationId == n.Id).ToList();
            var lastFailedAttempt = notificationAttempts
                .Where(a => !a.Success)
                .OrderByDescending(a => a.AttemptedAt)
                .FirstOrDefault();
            var lastSuccessfulAttempt = notificationAttempts
                .Where(a => a.Success)
                .OrderByDescending(a => a.AttemptedAt)
                .FirstOrDefault();

            return new CustomerNotificationHistoryItemDto
            {
                NotificationId = n.Id,
                CustomerId = request.CustomerId,
                TemplateId = n.TemplateKey ?? string.Empty,
                Channel = n.Channel.ToString(),
                Status = n.Status.ToString(),
                AttemptCount = notificationAttempts.Count,
                LastError = lastFailedAttempt?.ErrorMessage,
                CreatedAt = n.CreatedAt,
                ScheduledAt = n.SendAt,
                SentAt = lastSuccessfulAttempt?.AttemptedAt ?? n.SentAt,
                FailedAt = n.Status == NotificationStatus.Failed ? lastFailedAttempt?.AttemptedAt : null,
                RenderedPreview = GenerateRenderedPreview(n)
            };
        }).ToList();

        // Return paginated result
        return new PagedResult<CustomerNotificationHistoryItemDto>(
            items,
            request.Page,
            request.PageSize,
            totalItems);
    }

    private static string? GenerateRenderedPreview(Notification notification)
    {
        if (string.IsNullOrEmpty(notification.Subject) && string.IsNullOrEmpty(notification.Body))
        {
            return null;
        }

        var preview = "";
        if (!string.IsNullOrEmpty(notification.Subject))
        {
            preview = notification.Subject;
        }

        if (!string.IsNullOrEmpty(notification.Body))
        {
            if (!string.IsNullOrEmpty(preview))
            {
                preview += ": ";
            }
            var bodyPreview = notification.Body.Length > 100 
                ? notification.Body.Substring(0, 100) + "..." 
                : notification.Body;
            preview += bodyPreview;
        }

        return string.IsNullOrEmpty(preview) ? null : preview;
    }
}
