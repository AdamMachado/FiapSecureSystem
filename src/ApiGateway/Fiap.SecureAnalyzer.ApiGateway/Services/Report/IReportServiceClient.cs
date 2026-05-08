using Fiap.SecureAnalyzer.ApiGateway.Contracts.Responses;
using Fiap.SecureAnalyzer.ApiGateway.Services.Common;

namespace Fiap.SecureAnalyzer.ApiGateway.Services.Report;

public interface IReportServiceClient
{
    Task<ReportByAnalysisResponse> GetReportByAnalysisAsync(Guid analysisId, CancellationToken cancellationToken);

    Task<ReportByAnalysisResponse?> TryGetReportByAnalysisAsync(Guid analysisId, CancellationToken cancellationToken);

    Task<ServiceFileResponse> DownloadReportByAnalysisAsync(
        Guid analysisId,
        string format,
        CancellationToken cancellationToken);
}
