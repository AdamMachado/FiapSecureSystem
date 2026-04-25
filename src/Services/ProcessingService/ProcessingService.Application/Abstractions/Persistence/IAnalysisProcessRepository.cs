using ProcessingService.Domain.Entities;

namespace ProcessingService.Application.Abstractions.Persistence;

public interface IAnalysisProcessRepository
{
    Task AddAsync(AnalysisProcess analysisProcess, CancellationToken cancellationToken = default);
    Task<AnalysisProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AnalysisProcess?> GetByAnalysisRequestIdAsync(Guid analysisRequestId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAnalysisRequestIdAsync(Guid analysisRequestId, CancellationToken cancellationToken = default);
    void Update(AnalysisProcess analysisProcess);
}
