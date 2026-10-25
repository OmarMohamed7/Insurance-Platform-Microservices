using Notifications.Application;
using Notifications.Domain;

namespace Notifications.Infrastructure;

// Stand-in until a real channel (email, SMS, push) exists: the stored row is the record, this only logs.
public sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Notification notification, CancellationToken ct)
    {
        logger.LogInformation("[{Channel}] To {Recipient}: {Subject} - {Body}",
            notification.Channel, notification.Recipient, notification.Subject, notification.Body);

        return Task.CompletedTask;
    }
}
