using Microsoft.EntityFrameworkCore;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Domain.Entities;
using ReportService.Infrastructure.Persistence.Context;

namespace ReportService.Infrastructure.Persistence.Repositories;

public sealed class AnalysisReportRepository : IAnalysisReportRepository
{
    private readonly ReportDbContext _dbContext;

    public AnalysisReportRepository(ReportDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(AnalysisReport report, CancellationToken cancellationToken = default)
        => _dbContext.AnalysisReports.AddAsync(report, cancellationToken).AsTask();

    public Task<AnalysisReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default)
        => _dbContext.AnalysisReports
            .Include(x => x.Files)
            .FirstOrDefaultAsync(x => x.Id == reportId, cancellationToken);

    public Task<AnalysisReport?> GetByAnalysisRequestIdAsync(Guid analysisRequestId, CancellationToken cancellationToken = default)
        => _dbContext.AnalysisReports
            .Include(x => x.Files)
            .FirstOrDefaultAsync(x => x.AnalysisRequestId == analysisRequestId, cancellationToken);

    public void Update(AnalysisReport report)
        => _dbContext.AnalysisReports.Update(report);
}
