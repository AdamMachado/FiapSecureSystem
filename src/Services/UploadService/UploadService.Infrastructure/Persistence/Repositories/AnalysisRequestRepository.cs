using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Pagination;
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

    public Task<AnalysisRequest?> GetByIdForUserAsync(
        Guid id,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
        => _dbContext.AnalysisRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id && x.RequestedByUserId == requestedByUserId,
                cancellationToken);

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.AnalysisRequests.AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<AnalysisRequest>> ListByUserAsync(
        Guid requestedByUserId,
        PaginationParams paginationParams,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paginationParams);

        var query = _dbContext.AnalysisRequests
            .AsNoTracking()
            .Where(x => x.RequestedByUserId == requestedByUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
            return PagedResult<AnalysisRequest>.Empty(paginationParams.PageNumber, paginationParams.PageSize);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(paginationParams.Skip)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<AnalysisRequest>.Create(
            items,
            totalCount,
            paginationParams.PageNumber,
            paginationParams.PageSize);
    }

    public async Task<IReadOnlyCollection<AnalysisRequest>> ListByUserAndIdsAsync(
        Guid requestedByUserId,
        IReadOnlyCollection<Guid> analysisRequestIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysisRequestIds);

        var normalizedIds = analysisRequestIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedIds.Length == 0)
            return Array.Empty<AnalysisRequest>();

        return await _dbContext.AnalysisRequests
            .AsNoTracking()
            .Where(x => x.RequestedByUserId == requestedByUserId && normalizedIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Update(AnalysisRequest analysisRequest)
        => _dbContext.AnalysisRequests.Update(analysisRequest);
}
