using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Infrastructure.Persistence.Context;

namespace ProcessingService.Infrastructure.Persistence.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ProcessingDbContext _dbContext;

    public EfUnitOfWork(ProcessingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
