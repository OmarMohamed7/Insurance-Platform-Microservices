using System.Text.Json;
using Claims.Application;
using Claims.Domain;
using Claims.Infrastructure.Persistence;
using Contracts;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure;

public class UnitOfWork(ClaimsDbContext db): IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var trackedClaims = db.ChangeTracker
            .Entries<Claim>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var claim in trackedClaims)
        {
            foreach (var domainEvent in claim.DomainEvents)
            {
                if (domainEvent is not Domain.ClaimApproved approved)
                {
                    continue;
                }

                var contract = new ClaimApprovedContract(
                    EventId: Guid.NewGuid(),
                    CorrelationId: approved.ClaimId,
                    OccurredAt: approved.OccurredAt,
                    ClaimId: approved.ClaimId,
                    Amount: approved.Amount.Amount,
                    Currency: approved.Amount.Currency);

                db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = contract.EventId,
                    Type = nameof(ClaimApprovedContract),
                    Payload = JsonSerializer.Serialize(contract),
                    OccurredAt = contract.OccurredAt
                });
            }

            claim.ClearDomainEvents();
        }

        return db.SaveChangesAsync(cancellationToken);
    }
}
