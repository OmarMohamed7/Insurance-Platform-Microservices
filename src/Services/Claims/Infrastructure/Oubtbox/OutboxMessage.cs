namespace Claims.Infrastructure;

// An outbox message is a database row that stands in for an event you want to publish but haven't sent yet.

// The problem it solves. When an admin approves a claim, two things have to happen:
// the claim's status changes in Postgres, and ClaimApproved goes to RabbitMQ.
// These are two separate systems, so you can't make them atomic.
// - If you save the claim and then publish, a crash in between leaves the claim approved but the event never sent,
// so Payment never pays.
// - If you publish first and then save, the save can fail and Payment pays a claim that was never approved

// The fix. Don't publish during the request. In the same database transaction that updates the claim, insert a row describing the event:
//
// Claims table:        Status = Approved
// OutboxMessages:      Type = "ClaimApproved", Payload = {...json...}, ProcessedAt = null

//Then a separate background job (Hangfire, in your roadmap) does the sending:
// 1. Read rows where ProcessedAt is null.
// 2. Publish each to RabbitMQ.
public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; } =  string.Empty;
}