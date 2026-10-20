namespace Claims.Application.Handlers;

public sealed record ApproveClaimCommand(Guid ClaimId, string DecidedBy);

public sealed class ApproveClaimHandler(IClaimRepository claims, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<ClaimDto> Handle(ApproveClaimCommand command, CancellationToken ct)
    {
        var claim = await claims.GetByIdAsync(command.ClaimId, ct)
                    ?? throw new ClaimNotFoundException(command.ClaimId);

        claim.Approve(command.DecidedBy, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveChangesAsync(ct);
        
        return ClaimDto.From(claim);
    }
}
