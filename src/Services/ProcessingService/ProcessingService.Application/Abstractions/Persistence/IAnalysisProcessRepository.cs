using ProcessingService.Domain.Entities;
using ProcessingService.Domain.ValueObjects;

namespace ProcessingService.Application.Abstractions.Persistence;

public interface IAnalysisProcessRepository
{
    Task AddAsync(AnalysisProcess analysisProcess, CancellationToken cancellationToken = default);
    Task<AnalysisProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AnalysisProcess?> GetByAnalysisRequestIdAsync(AnalysisRequestId analysisRequestId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAnalysisRequestIdAsync(AnalysisRequestId analysisRequestId, CancellationToken cancellationToken = default);
    void Update(AnalysisProcess analysisProcess);
}
