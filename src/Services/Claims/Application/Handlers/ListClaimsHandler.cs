using Claims.Domain;

namespace Claims.Application.Handlers;

public sealed record ListClaimsQuery(ClaimStatus? Status, int Page = 1, int PageSize = 20);

public sealed class ListClaimsHandler(IClaimRepository claims)
{
    private const int MaxPageSize = 100;

    public async Task<IReadOnlyList<ClaimDto>> Handle(ListClaimsQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var result = await claims.ListAsync(query.Status, (page - 1) * pageSize, pageSize, ct);
        return [.. result.Select(ClaimDto.From)];
    }
}
