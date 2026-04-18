using Microsoft.EntityFrameworkCore;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Domain.Entities;
using UploadService.Infrastructure.Persistence.Context;

namespace UploadService.Infrastructure.Persistence.Repositories;

public sealed class AnalysisRequestRepository : IAnalysisRequestRepository
{
    private readonly UploadDbContext _dbContext;

    public AnalysisRequestRepository(UploadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken = default)
        => _dbContext.AnalysisRequests.AddAsync(analysisRequest, cancellationToken).AsTask();

    public Task<AnalysisRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.AnalysisRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.AnalysisRequests.AnyAsync(x => x.Id == id, cancellationToken);

    public void Update(AnalysisRequest analysisRequest)
        => _dbContext.AnalysisRequests.Update(analysisRequest);
}
