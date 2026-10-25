using Notifications.Domain;

namespace Notifications.Application;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Notification>> ListAsync(Guid? claimId, int skip, int take, CancellationToken ct);
    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken ct);
    Task AddAsync(Notification notification, CancellationToken ct);
}
