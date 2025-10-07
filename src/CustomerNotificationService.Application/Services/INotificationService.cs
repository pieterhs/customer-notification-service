namespace CustomerNotificationService.Application.Services;

using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;

public record SendNotificationRequest(
    string Recipient,
    string? TemplateKey,
    string? Subject,
    string? Body,
    string? PayloadJson,
    ChannelType Channel,
    DateTimeOffset? SendAt = null,
    string? CustomerId = null,
    string? IdempotencyKey = null
);

public interface INotificationService
{
    Task<SendNotificationResponse> SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves paginated customer notification history with optional filters
    /// </summary>
    /// <param name="request">The request containing customer ID, filters, and pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of customer notification history items</returns>
    Task<PagedResult<CustomerNotificationHistoryItemDto>> GetCustomerNotificationHistoryAsync(CustomerNotificationHistoryRequest request, CancellationToken cancellationToken = default);
}
