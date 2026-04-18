using UploadService.Application.Abstractions.Persistence;
using UploadService.Infrastructure.Persistence.Context;

namespace UploadService.Infrastructure.Persistence.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly UploadDbContext _dbContext;

    public EfUnitOfWork(UploadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
