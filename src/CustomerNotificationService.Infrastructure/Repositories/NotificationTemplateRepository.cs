using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Infrastructure.Repositories;

public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly AppDbContext _db;
    public NotificationTemplateRepository(AppDbContext db) => _db = db;

    public Task<List<NotificationTemplate>> GetAllAsync(CancellationToken ct) =>
        _db.NotificationTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(NotificationTemplate template, CancellationToken ct)
    {
        await _db.NotificationTemplates.AddAsync(template, ct);
        await _db.SaveChangesAsync(ct);
        // Detach to avoid tracking conflicts in tests that reuse the same DbContext
        _db.Entry(template).State = EntityState.Detached;
    }

    public async Task UpdateAsync(NotificationTemplate template, CancellationToken ct)
    {
        var tracked = await _db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == template.Id, ct);
        if (tracked != null)
        {
            _db.Entry(template).State = EntityState.Modified;
        }
        else
        {
            _db.NotificationTemplates.Update(template);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(NotificationTemplate template, CancellationToken ct)
    {
        var tracked = await _db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == template.Id, ct);
        if (tracked != null)
        {
            _db.Entry(template).State = EntityState.Deleted;
        }
        else
        {
            _db.NotificationTemplates.Remove(template);
        }
        await _db.SaveChangesAsync(ct);
    }
}
