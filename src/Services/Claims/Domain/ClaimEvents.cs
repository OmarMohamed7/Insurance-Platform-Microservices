namespace Claims.Domain;

public sealed record ClaimSubmitted(Guid ClaimId, string PolicyNumber, string ClaimantId, Money Amount, DateTime OccurredAt) : IDomainEvent;

public sealed record StartReviewClaimEvent(
    Guid Id,
    DateTime OccurredAt
): IDomainEvent;

public sealed record ClaimApproved(Guid ClaimId, string ClaimantId, Money Amount, DateTime OccurredAt) : IDomainEvent;

public sealed record ClaimRejected(Guid ClaimId, string ClaimantId, string Reason, DateTime OccurredAt) : IDomainEvent;
