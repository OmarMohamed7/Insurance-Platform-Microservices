using System.Text.Json;
using Claims.Application;
using Claims.Domain;
using Claims.Infrastructure.Outbox;
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
                var eventId = Guid.NewGuid();
                var contract = ToContract(domainEvent, eventId);
                if (contract is null)
                {
                    continue;
                }

                var contractType = contract.GetType();

                db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = eventId,
                    Type = contractType.Name,
                    Payload = JsonSerializer.Serialize(contract, contractType),
                    OccurredAt = domainEvent.OccurredAt
                });
            }

            claim.ClearDomainEvents();
        }

        return db.SaveChangesAsync(cancellationToken);
    }

    // The single place that decides which domain events leave the service. Returns null for internal-only events.
    private static object? ToContract(IDomainEvent domainEvent, Guid eventId) => domainEvent switch
    {
        ClaimSubmitted e => new ClaimSubmittedContract(
            EventId: eventId,
            CorrelationId: e.ClaimId,
            OccurredAt: e.OccurredAt,
            ClaimId: e.ClaimId,
            PolicyNumber: e.PolicyNumber,
            ClaimantId: e.ClaimantId,
            Amount: e.Amount.Amount,
            Currency: e.Amount.Currency),

        ClaimApproved e => new ClaimApprovedContract(
            EventId: eventId,
            CorrelationId: e.ClaimId,
            OccurredAt: e.OccurredAt,
            ClaimId: e.ClaimId,
            Amount: e.Amount.Amount,
            Currency: e.Amount.Currency),

        ClaimRejected e => new ClaimRejectedContract(
            EventId: eventId,
            CorrelationId: e.ClaimId,
            OccurredAt: e.OccurredAt,
            ClaimId: e.ClaimId,
            Reason: e.Reason),

        _ => null
    };
}
