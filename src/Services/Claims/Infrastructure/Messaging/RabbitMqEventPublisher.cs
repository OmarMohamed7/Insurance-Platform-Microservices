using System.Text;
using Claims.Application;
using Microsoft.EntityFrameworkCore.Metadata;
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

public class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher, IDisposable
{   
    private readonly RabbitMqOptions _options = options.Value;
    private readonly ConnectionFactory _factory;
    private IConnection _connection;
    private IChannel _channel;

    // Using a SemaphoreSlim to prevent race conditions during lazy initialization
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    private const string ExchangeName = "application-outbox-exchange";
    

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
            mandatory: false,
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
            await _connection.DisposeAsync();

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

            await _channel.ExchangeDeclareAsync(
                _options.Exchange, ExchangeType.Topic, durable: true, cancellationToken:
                ct);

            return _channel;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _channel.Dispose();
        _lock.Dispose();
    }
}