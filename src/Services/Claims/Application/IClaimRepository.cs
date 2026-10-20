using Claims.Domain;

namespace Claims.Application;

public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Claim>> ListAsync(ClaimStatus? status, int skip, int take, CancellationToken ct);
    Task AddAsync(Claim claim, CancellationToken ct);
}
