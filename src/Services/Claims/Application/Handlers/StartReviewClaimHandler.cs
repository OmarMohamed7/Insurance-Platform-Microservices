namespace Claims.Application;

public sealed record StartReviewClaimCommand(Guid ClaimId);

public sealed class StartReviewClaimHandler(IClaimRepository claims, IUnitOfWork unitOfWork)
{
    public async Task<ClaimDto> Handle(StartReviewClaimCommand command, CancellationToken ct)
    {
        var claim = await claims.GetByIdAsync(command.ClaimId, ct)
                    ?? throw new ClaimNotFoundException(command.ClaimId);

        claim.StartReview();

        await unitOfWork.SaveChangesAsync(ct);

        return ClaimDto.From(claim);
    }
}
