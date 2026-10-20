namespace Claims.Application;

public sealed class ClaimNotFoundException(Guid claimId)
    : Exception($"Claim {claimId} was not found.")
{
    public Guid ClaimId { get; } = claimId;
}
