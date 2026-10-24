namespace Contracts;

public sealed record ClaimRejectedContract(
    Guid EventId,
    Guid CorrelationId,
    DateTime OccurredAt,
    Guid ClaimId,
    string Reason,
    string ClaimantId);
