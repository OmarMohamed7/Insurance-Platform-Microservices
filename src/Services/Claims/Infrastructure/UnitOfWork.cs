using Claims.Application;
using Claims.Infrastructure.Persistence;

namespace Claims.Infrastructure;

public class UnitOfWork(ClaimsDbContext db): IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}