using ReportService.Domain.Entities;

namespace ReportService.Application.Abstractions.Persistence;

public interface IAnalysisReportRepository
{
    Task AddAsync(AnalysisReport report, CancellationToken cancellationToken = default);
    Task<AnalysisReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<AnalysisReport?> GetByAnalysisRequestIdAsync(Guid analysisRequestId, CancellationToken cancellationToken = default);
    void Update(AnalysisReport report);
}
