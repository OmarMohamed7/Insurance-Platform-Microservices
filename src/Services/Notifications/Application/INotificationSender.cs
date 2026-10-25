using Notifications.Domain;

namespace Notifications.Application;

public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken ct);
}
