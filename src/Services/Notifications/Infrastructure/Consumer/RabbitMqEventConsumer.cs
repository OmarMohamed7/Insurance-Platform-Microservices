using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notifications.Application.Handlers;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notifications.Infrastructure.Consumer;

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "insurance.events";
}

// Transport only: read a delivery, pick the contract type, hand it to the application handler, ack or nack.
public sealed class RabbitMqEventConsumer(
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqEventConsumer> logger) : BackgroundService
{
    private const string QueueName = "notification.events";

    // The publisher uses the contract type name as the routing key.
    private static readonly string[] RoutingKeys =
    [
        nameof(ClaimSubmittedContract),
        nameof(ClaimApprovedContract),
        nameof(ClaimRejectedContract)
    ];

    private static readonly TimeSpan ConnectRetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            _connection = await ConnectAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

            await DeclareTopologyAsync(_channel, ct);

            // At most 10 unacked messages in flight, so one slow handler does not drain the queue.
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: ct);

            var channel = _channel;
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, delivery) => OnDeliveryAsync(channel, delivery, ct);

            // autoAck must be false: we ack ourselves, after the notification is saved.
            await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, ct);

            logger.LogInformation("Consuming {Queue} (keys: {Keys})", QueueName, string.Join(", ", RoutingKeys));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    // The exchange is shared with Claims; the queue and the bindings belong to this service.
    private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

        foreach (var key in RoutingKeys)
            await channel.QueueBindAsync(QueueName, _options.Exchange, key, cancellationToken: ct);
    }

    private async Task OnDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        try
        {
            var contract = Deserialize(delivery.RoutingKey, delivery.Body);

            if (contract is null)
            {
                logger.LogError("Unknown routing key {RoutingKey}; message dropped", delivery.RoutingKey);
                await NackAsync(channel, delivery, requeue: false);
                return;
            }

            // The consumer is a singleton but the DbContext is scoped, so every message gets its own scope.
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ClaimEventNotificationHandler>();
            await handler.HandleAsync(contract, ct);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two deliveries of the same event raced each other; the unique index on EventId caught the second.
            logger.LogInformation("Duplicate event on {RoutingKey}; already processed", delivery.RoutingKey);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
        }
        catch (Exception e) when (e is JsonException or ArgumentException)
        {
            // Retrying a malformed message will never fix it.
            logger.LogError(e, "Invalid message on {RoutingKey}; message dropped", delivery.RoutingKey);
            await NackAsync(channel, delivery, requeue: false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down: leave it unacked, the broker redelivers it to the next instance.
        }
        catch (Exception e)
        {
            // Probably transient (database down): put it back on the queue.
            logger.LogError(e, "Failed to process message on {RoutingKey}; requeued", delivery.RoutingKey);
            await NackAsync(channel, delivery, requeue: true);
        }
    }

    private static object? Deserialize(string routingKey, ReadOnlyMemory<byte> body) => routingKey switch
    {
        nameof(ClaimSubmittedContract) => JsonSerializer.Deserialize<ClaimSubmittedContract>(body.Span),
        nameof(ClaimApprovedContract) => JsonSerializer.Deserialize<ClaimApprovedContract>(body.Span),
        nameof(ClaimRejectedContract) => JsonSerializer.Deserialize<ClaimRejectedContract>(body.Span),
        _ => null
    };

    // Negative acknowledgement: "I could not process this". requeue=true puts it back, false discards it.
    private async Task NackAsync(IChannel channel, BasicDeliverEventArgs delivery, bool requeue)
    {
        try
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Could not nack delivery {DeliveryTag}", delivery.DeliveryTag);
        }
    }

    // RabbitMQ may still be starting (docker compose), so keep trying instead of crashing the host.
    private async Task<IConnection> ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password
        };

        while (true)
        {
            try
            {
                return await factory.CreateConnectionAsync(ct);
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(e, "RabbitMQ at {Host}:{Port} not reachable; retrying in {Delay}s",
                    _options.Host, _options.Port, ConnectRetryDelay.TotalSeconds);
                await Task.Delay(ConnectRetryDelay, ct);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
