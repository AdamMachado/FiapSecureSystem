using Fiap.SecureSystem.ApiGateway.Contracts.Responses;
using Fiap.SecureSystem.ApiGateway.Services.Common;

namespace Fiap.SecureSystem.ApiGateway.Services.Report;

public interface IReportServiceClient
{
    Task<ReportByAnalysisResponse> GetReportByAnalysisAsync(Guid analysisId, CancellationToken cancellationToken);

    Task<ReportByAnalysisResponse?> TryGetReportByAnalysisAsync(Guid analysisId, CancellationToken cancellationToken);

    Task<ServiceFileResponse> DownloadReportByAnalysisAsync(
        Guid analysisId,
        string format,
        CancellationToken cancellationToken);
}
