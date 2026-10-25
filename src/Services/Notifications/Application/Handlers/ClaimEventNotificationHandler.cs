using Contracts;
using Notifications.Domain;

namespace Notifications.Application.Handlers;

// Turns a claim event into a stored notification. Safe to call twice with the same event: the second call does nothing.
public sealed class ClaimEventNotificationHandler(
    INotificationRepository repository,
    IUnitOfWork unitOfWork,
    INotificationSender sender)
{
    public async Task HandleAsync(object contract, CancellationToken ct)
    {
        var notification = Map(contract);

        if (await repository.ExistsByEventIdAsync(notification.EventId, ct))
            return;

        await repository.AddAsync(notification, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Send only after the row is saved, so a notification is never delivered without being recorded.
        await sender.SendAsync(notification, ct);
    }

    public static Notification Map(object contract) => contract switch
    {
        ClaimSubmittedContract e => Notification.Create(
            e.EventId, e.ClaimId, e.ClaimantId, nameof(ClaimSubmittedContract),
            "Claim received",
            $"Your claim {e.ClaimId} for {e.Amount} {e.Currency} was received.",
            e.OccurredAt),

        ClaimApprovedContract e => Notification.Create(
            e.EventId, e.ClaimId, e.ClaimantId, nameof(ClaimApprovedContract),
            "Claim approved",
            $"Your claim {e.ClaimId} for {e.Amount} {e.Currency} was approved.",
            e.OccurredAt),

        ClaimRejectedContract e => Notification.Create(
            e.EventId, e.ClaimId, e.ClaimantId, nameof(ClaimRejectedContract),
            "Claim rejected",
            $"Your claim {e.ClaimId} was rejected: {e.Reason}.",
            e.OccurredAt),

        _ => throw new ArgumentException($"No notification for {contract.GetType().Name}.", nameof(contract))
    };
}
