using FiapSecureSystem.UploadOrchestration.Domain.Entities;

namespace FiapSecureSystem.UploadOrchestration.Application.Abstractions;

public interface IAnalysisRequestRepository
{
    Task AddAsync(AnalysisRequest request, CancellationToken cancellationToken);
    Task<AnalysisRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}