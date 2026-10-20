namespace Claims.Domain;

public enum DecisionOutcome { Approved, Rejected }

public sealed record ClaimDecision(DecisionOutcome Outcome, string? Reason, string DecidedBy, DateTime DecidedAt)
{
    public static ClaimDecision Approved(string? reason, string decidedBy, DateTime decidedAt)
    {
        return new ClaimDecision(DecisionOutcome.Approved, reason, decidedBy, decidedAt);
    }

    public static ClaimDecision Rejected(string reason, string decidedBy, DateTime decidedAt)
    {
        return new ClaimDecision(DecisionOutcome.Rejected, reason, decidedBy, decidedAt);
    }
}
