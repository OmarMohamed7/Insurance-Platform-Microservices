using Claims.Infrastructure;
using Claims.Infrastructure.Outbox;

namespace Claims.Application;

public interface IEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct);
}