using Notifications.Application;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure;

public sealed class UnitOfWork(NotificationDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
