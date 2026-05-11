using Fiap.SecureSystem.ApiGateway.Contracts.Responses;
using Fiap.SecureSystem.ApiGateway.Services.Common;
using Shared.Kernel.Pagination;

namespace Fiap.SecureSystem.ApiGateway.Services.Upload;

public interface IUploadServiceClient
{
    Task<CreateAnalysisResponse> UploadAnalysisAsync(IFormFile file, CancellationToken cancellationToken);

    Task<PagedResult<AnalysisSummaryResponse>> ListAnalysesAsync(
        PaginationParams paginationParams,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AnalysisSummaryResponse>> GetAnalysesByIdsAsync(
        IReadOnlyCollection<Guid> analysisRequestIds,
        CancellationToken cancellationToken);

    Task<AnalysisStatusResponse> GetAnalysisAsync(Guid analysisRequestId, CancellationToken cancellationToken);

    Task<ServiceFileResponse> DownloadAnalysisAssetAsync(Guid analysisRequestId, CancellationToken cancellationToken);
}
