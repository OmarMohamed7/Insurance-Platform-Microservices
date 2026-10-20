using Claims.Domain;

namespace Claims.Application.Handlers;
// These Handlers grouped by use case
// All Handlers Grouped together act as a SERVICE

public sealed class SubmitClaimHandler(IClaimRepository claims, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<Guid> Handle(SubmitClaimCommand command, CancellationToken ct)
    {
        var amount = Money.Create(command.Amount, command.Currency);
        var claim = Claim.Submit(
            command.PolicyNumber,
            command.ClaimantId,
            command.Description,
            amount,
            clock.GetUtcNow().UtcDateTime);

        await claims.AddAsync(claim, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return claim.Id;
    }
}
