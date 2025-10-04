using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Api.Controllers
{
    [ApiController]
    [Route("api/auditlogs")]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public AuditLogsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{notificationId}")]
        public async Task<IActionResult> GetByNotificationId(Guid notificationId)
        {
            var logs = await _dbContext.AuditLogs
                .Where(a => a.NotificationId == notificationId)
                .OrderBy(a => a.Timestamp)
                .Select(a => new
                {
                    a.Timestamp,
                    a.Action,
                    a.Details
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
