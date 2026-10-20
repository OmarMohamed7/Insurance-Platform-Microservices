namespace Claims.Application;

public sealed record SubmitClaimCommand(string PolicyNumber, string ClaimantId, string Description, decimal Amount, string Currency);
