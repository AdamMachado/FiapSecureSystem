using FiapSecureSystem.UploadOrchestration.Application.Abstractions;
using FiapSecureSystem.UploadOrchestration.Domain.Entities;
using FiapSecureSystem.UploadOrchestration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiapSecureSystem.UploadOrchestration.Infrastructure.Repositories;

public class AnalysisRequestRepository : IAnalysisRequestRepository
{
    private readonly UploadDbContext _context;

    public AnalysisRequestRepository(UploadDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        await _context.AnalysisRequests.AddAsync(request, cancellationToken);
    }

    public async Task<AnalysisRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.AnalysisRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}