namespace Claims.Application.Handlers;

public sealed record RejectClaimRequest(string Reason);

public sealed record RejectClaimCommand(Guid ClaimId, string Reason, string DecidedBy);

public sealed class RejectClaimHandler(IClaimRepository claims, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<ClaimDto> Handle(RejectClaimCommand command, CancellationToken ct)
    {
        var claim = await claims.GetByIdAsync(command.ClaimId, ct)
                    ?? throw new ClaimNotFoundException(command.ClaimId);

        claim.Reject(command.Reason, command.DecidedBy, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveChangesAsync(ct);
        
        return ClaimDto.From(claim);
    }
}
