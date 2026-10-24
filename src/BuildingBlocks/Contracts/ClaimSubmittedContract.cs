namespace Contracts;

public sealed record ClaimSubmittedContract(
    Guid EventId,
    Guid CorrelationId,
    DateTime OccurredAt,
    Guid ClaimId,
    string PolicyNumber,
    string ClaimantId,
    decimal Amount,
    string Currency);
