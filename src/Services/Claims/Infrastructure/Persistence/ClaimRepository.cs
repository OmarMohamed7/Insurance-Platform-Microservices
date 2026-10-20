using Claims.Application;
using Claims.Domain;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence;

public sealed class ClaimRepository(ClaimsDbContext db) : IClaimRepository
{
    public Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Claims.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Claim>> ListAsync(ClaimStatus? status, int skip, int take, CancellationToken ct)
    {
        var query = db.Claims.AsNoTracking();

        if (status is not null)
            query = query.Where(c => c.Status == status);

        return await query
            .OrderByDescending(c => c.SubmittedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Claim claim, CancellationToken ct) =>
        await db.Claims.AddAsync(claim, ct);
}
