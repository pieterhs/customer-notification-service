namespace CustomerNotificationService.Application.Services;

using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Application.Dtos;

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
    Task<PagedResult<CustomerNotificationHistoryItemDto>> GetCustomerNotificationHistoryAsync(CustomerNotificationHistoryRequest request, CancellationToken cancellationToken = default);
}
