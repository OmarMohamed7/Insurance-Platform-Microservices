namespace Contracts;

public sealed record ClaimApprovedContract(
    Guid EventId,
    Guid CorrelationId,
    DateTime OccurredAt,
    Guid ClaimId,
    decimal Amount,
    string Currency);
