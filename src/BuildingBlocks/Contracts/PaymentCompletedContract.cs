namespace Contracts;

public sealed record PaymentCompletedContract(
    Guid EventId,
    Guid CorrelationId,
    DateTime OccurredAt,
    Guid ClaimId,
    Guid PaymentId,
    decimal Amount,
    string Currency);
