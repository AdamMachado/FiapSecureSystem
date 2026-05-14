using Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway;

public interface IApiGatewayClient
{
    Task<PagedResult<AnalysisSummaryResponse>> ListAnalysesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<AnalysisDetailsResponse> GetAnalysisDetailsAsync(Guid analysisId, CancellationToken cancellationToken);
    Task<CreateAnalysisResponse> UploadAnalysisAsync(IFormFile file, CancellationToken cancellationToken);
    Task<ReportByAnalysisResponse> GetReportByAnalysisAsync(Guid analysisId, CancellationToken cancellationToken);
    Task<ApiGatewayFileResponse> DownloadReportAsync(Guid analysisId, string format, CancellationToken cancellationToken);
    Task<ApiGatewayFileResponse> DownloadAnalysisAssetAsync(Guid analysisId, CancellationToken cancellationToken);
}
