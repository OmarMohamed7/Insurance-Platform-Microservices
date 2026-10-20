namespace Claims.Domain;

public sealed class InvalidClaimTransitionException(ClaimStatus from, ClaimStatus to)
    : Exception($"Cannot move a claim from {from} to {to}.")
{
    public ClaimStatus From { get; } = from;
    public ClaimStatus To { get; } = to;
}
