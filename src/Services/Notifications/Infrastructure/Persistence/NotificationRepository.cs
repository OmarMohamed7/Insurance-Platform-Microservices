using Microsoft.EntityFrameworkCore;
using Notifications.Application;
using Notifications.Domain;

namespace Notifications.Infrastructure.Persistence;

public sealed class NotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<Notification>> ListAsync(Guid? claimId, int skip, int take, CancellationToken ct)
    {
        var query = db.Notifications.AsNoTracking();

        if (claimId is not null)
            query = query.Where(n => n.ClaimId == claimId);

        return await query
            .OrderByDescending(n => n.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken ct) =>
        db.Notifications.AnyAsync(n => n.EventId == eventId, ct);

    public async Task AddAsync(Notification notification, CancellationToken ct) =>
        await db.Notifications.AddAsync(notification, ct);
}
