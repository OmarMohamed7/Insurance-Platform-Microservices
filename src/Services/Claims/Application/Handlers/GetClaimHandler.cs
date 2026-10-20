namespace Claims.Application.Handlers;

public sealed record GetClaimQueryCommand(Guid ClaimId);

public sealed class GetClaimHandler(IClaimRepository claims)
{
    public async Task<ClaimDto?> Handle(GetClaimQueryCommand query, CancellationToken ct)
    {
        var claim = await claims.GetByIdAsync(query.ClaimId, ct);
        return claim is null ? null : ClaimDto.From(claim);
    }
}
