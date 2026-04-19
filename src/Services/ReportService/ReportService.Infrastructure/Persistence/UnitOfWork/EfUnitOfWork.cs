using ReportService.Application.Abstractions.Persistence;
using ReportService.Infrastructure.Persistence.Context;

namespace ReportService.Infrastructure.Persistence.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ReportDbContext _dbContext;

    public EfUnitOfWork(ReportDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}