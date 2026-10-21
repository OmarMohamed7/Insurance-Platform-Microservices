using Claims.Infrastructure;

namespace Claims.Application;

public interface IEventPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct);
}