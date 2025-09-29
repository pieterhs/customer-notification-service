using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using System.Collections.Concurrent;

namespace CustomerNotificationService.Infrastructure.Services;

public class InMemoryQueueService : IQueueService
{
    private readonly ConcurrentQueue<NotificationQueueItem> _queue = new();

    public Task EnqueueAsync(NotificationQueueItem item, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(item);
        return Task.CompletedTask;
    }

    public Task<NotificationQueueItem?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        _queue.TryDequeue(out var item);
        return Task.FromResult(item);
    }
}
