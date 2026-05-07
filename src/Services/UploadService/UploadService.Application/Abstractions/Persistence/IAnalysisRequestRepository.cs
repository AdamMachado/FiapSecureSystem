using Shared.Kernel.Pagination;
using UploadService.Domain.Entities;

namespace UploadService.Application.Abstractions.Persistence;

public interface IAnalysisRequestRepository
{
    Task AddAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken = default);
    Task<AnalysisRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AnalysisRequest?> GetByIdForUserAsync(Guid id, Guid requestedByUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<AnalysisRequest>> ListByUserAsync(
        Guid requestedByUserId,
        PaginationParams paginationParams,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AnalysisRequest>> ListByUserAndIdsAsync(
        Guid requestedByUserId,
        IReadOnlyCollection<Guid> analysisRequestIds,
        CancellationToken cancellationToken = default);
    void Update(AnalysisRequest analysisRequest);
}
