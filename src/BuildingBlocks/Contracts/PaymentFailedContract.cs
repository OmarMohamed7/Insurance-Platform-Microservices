namespace Contracts;

public sealed record PaymentFailedContract(
    Guid EventId,
    Guid CorrelationId,
    DateTime OccurredAt,
    Guid ClaimId,
    string Reason);
