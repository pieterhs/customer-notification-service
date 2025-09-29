using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Infrastructure.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly AppDbContext _db;
    public TemplateRepository(AppDbContext db) => _db = db;

    public Task<Template?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        => _db.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Key == key, cancellationToken);
}
