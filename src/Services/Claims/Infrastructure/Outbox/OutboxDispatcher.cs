using Claims.Application;
using Claims.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Outbox;

// Polls the outbox and publishes unsent rows. Delivery is at-least-once: a crash after publishing but before
// the commit means the row is sent again, which is why consumers need an inbox.
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 10;
    private const int MaxErrorLength = 75; // matches the Error column
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            try
            {
                // Keep draining while full batches come back, so a backlog doesn't wait one interval per batch.
                while (await DispatchBatchAsync(stoppingToken) == BatchSize) { }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Outbox dispatch cycle failed");
            }
        } while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<int> DispatchBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // SKIP LOCKED lets several instances share the outbox without publishing the same row twice.
        var messages = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAt" IS NULL AND "Attempts" < {0}
                ORDER BY "OccurredAt"
                LIMIT {1}
                FOR UPDATE SKIP LOCKED
                """, MaxAttempts, BatchSize)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message, ct);
                message.ProcessedAt = timeProvider.GetUtcNow().UtcDateTime;
                message.Error = null;
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                message.Attempts++;
                message.Error = e.Message.Length > MaxErrorLength ? e.Message[..MaxErrorLength] : e.Message;
                logger.LogWarning(e, "Failed to publish outbox message {MessageId} (attempt {Attempts})",
                    message.Id, message.Attempts);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return messages.Count;
    }
}
