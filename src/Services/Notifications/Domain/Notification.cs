namespace Notifications.Domain;

public sealed class Notification
{
    public const string DemoChannel = "Demo";

    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid ClaimId { get; private set; }
    public string Recipient { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string Channel { get; private set; } = null!;
    public DateTime OccurredAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid eventId, Guid claimId, string recipient, string type, string subject, string body, DateTime occurredAt)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event id is required.", nameof(eventId));

        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim id is required.", nameof(claimId));

        RequireText(recipient, nameof(recipient));
        RequireText(type, nameof(type));
        RequireText(subject, nameof(subject));
        RequireText(body, nameof(body));

        return new Notification
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ClaimId = claimId,
            Recipient = recipient.Trim(),
            Type = type,
            Subject = subject,
            Body = body,
            Channel = DemoChannel,
            OccurredAt = occurredAt
        };
    }

    private static void RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required.", name);
    }
}
