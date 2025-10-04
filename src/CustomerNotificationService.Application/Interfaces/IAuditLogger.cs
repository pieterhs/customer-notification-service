using System;
using System.Threading.Tasks;

namespace CustomerNotificationService.Application.Interfaces
{
    public interface IAuditLogger
    {
        Task LogAsync(string action, Guid? notificationId, string? details);
    }
}
