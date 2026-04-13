using UploadService.Domain.Entities;

namespace UploadService.Application.Abstractions.Persistence;

public interface IAnalysisRequestRepository
{
    Task AddAsync(AnalysisRequest analysisRequest, CancellationToken cancellationToken = default);
    Task<AnalysisRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Update(AnalysisRequest analysisRequest);
}