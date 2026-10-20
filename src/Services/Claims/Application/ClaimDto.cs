using Claims.Domain;

namespace Claims.Application;

public sealed record ClaimDto(
    Guid Id,
    string PolicyNumber,
    string ClaimantId,
    string Description,
    decimal Amount,
    string Currency,
    string Status,
    DateTime SubmittedAt,
    ClaimDecisionDto? Decision)
{
    public static ClaimDto From(Claim claim) => new(
        claim.Id,
        claim.PolicyNumber,
        claim.ClaimantId,
        claim.Description,
        claim.Amount.Amount,
        claim.Amount.Currency,
        claim.Status.ToString(),
        claim.SubmittedAt,
        claim.Decision is null
            ? null
            : new ClaimDecisionDto(
                claim.Decision.Outcome.ToString(),
                claim.Decision.Reason,
                claim.Decision.DecidedBy,
                claim.Decision.DecidedAt));
}

public sealed record ClaimDecisionDto(string Outcome, string? Reason, string DecidedBy, DateTime DecidedAt);
