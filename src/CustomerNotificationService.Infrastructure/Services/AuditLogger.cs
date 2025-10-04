using System;
using System.Threading.Tasks;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Infrastructure.Data;

namespace CustomerNotificationService.Infrastructure.Services
{
    public class AuditLogger : Application.Interfaces.IAuditLogger
    {
        private readonly AppDbContext _dbContext;

        public AuditLogger(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task LogAsync(string action, Guid? notificationId, string? details)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Action = action,
                NotificationId = notificationId,
                Details = details
            };
            await _dbContext.AuditLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }
    }
}
