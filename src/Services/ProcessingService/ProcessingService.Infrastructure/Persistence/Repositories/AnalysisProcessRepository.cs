using Microsoft.EntityFrameworkCore;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.ValueObjects;
using ProcessingService.Infrastructure.Persistence.Context;

namespace ProcessingService.Infrastructure.Persistence.Repositories;

public sealed class AnalysisProcessRepository : IAnalysisProcessRepository
{
    private readonly ProcessingDbContext _dbContext;

    public AnalysisProcessRepository(ProcessingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AnalysisProcess analysisProcess, CancellationToken cancellationToken = default)
    {
        await _dbContext.AnalysisProcesses.AddAsync(analysisProcess, cancellationToken).AsTask();
    }

    public async Task<AnalysisProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AnalysisProcesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<AnalysisProcess?> GetByAnalysisRequestIdAsync(
        AnalysisRequestId analysisRequestId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AnalysisProcesses
            .FirstOrDefaultAsync(x => x.AnalysisRequestId == analysisRequestId, cancellationToken);
    }

    public async Task<bool> ExistsByAnalysisRequestIdAsync(
        AnalysisRequestId analysisRequestId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AnalysisProcesses.AnyAsync(x => x.AnalysisRequestId == analysisRequestId, cancellationToken);
    }

    public void Update(AnalysisProcess analysisProcess)
    {
        _dbContext.AnalysisProcesses.Update(analysisProcess);
    }
}
