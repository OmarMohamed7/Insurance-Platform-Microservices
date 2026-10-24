using System.Text;
using Claims.Application;
using Claims.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Claims.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "insurance.events";
}

public class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    // Using a SemaphoreSlim to prevent race conditions during lazy initialization
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        var channel = await GetChannelAsync(ct);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.Id.ToString(),
            Type = message.Type,
        };

        // With publisher confirms on, this awaits the broker's ack and throws if it is not confirmed
        await channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: message.Type,
            mandatory: true,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(message.Payload),
            cancellationToken: ct
            );
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await _lock.WaitAsync(ct);

        try
        {
            if (_channel is { IsOpen: true }) return _channel;

            await CloseAsync();

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Password,
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                ct);

            // mandatory: true makes the broker return messages no queue is bound to
            _channel.BasicReturnAsync += (_, args) =>
            {
                logger.LogError(
                    "RabbitMQ could not route message {MessageId} (routing key {RoutingKey}): {ReplyCode} {ReplyText}",
                    args.BasicProperties.MessageId, args.RoutingKey, args.ReplyCode, args.ReplyText);
                return Task.CompletedTask;
            };

            await _channel.ExchangeDeclareAsync(
                _options.Exchange, ExchangeType.Topic, durable: true, cancellationToken:
                ct);

            return _channel;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to connect to RabbitMQ at {Host}:{Port}", _options.Host, _options.Port);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task CloseAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync(); 
        if (_connection is not null) await _connection.DisposeAsync();
        _channel = null;
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _lock.Dispose();
    }
}
