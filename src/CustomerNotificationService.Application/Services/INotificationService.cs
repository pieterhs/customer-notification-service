namespace CustomerNotificationService.Application.Services;

using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;

public record SendNotificationRequest(
    string Recipient,
    string? TemplateKey,
    string? Subject,
    string? Body,
    string? PayloadJson,
    ChannelType Channel,
    DateTimeOffset? SendAt = null,
    string? CustomerId = null
);

public interface INotificationService
{
    Task<Guid> SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);
}
