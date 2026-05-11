using System.Net;
using Fiap.SecureAnalyzer.ApiGateway.Contracts.Responses;
using Fiap.SecureAnalyzer.ApiGateway.Serialization;
using Fiap.SecureAnalyzer.ApiGateway.Services.Common;

namespace Fiap.SecureAnalyzer.ApiGateway.Services.Report;

public sealed class ReportServiceClient : IReportServiceClient
{
    private const string ServiceName = "ReportService.Api";
    private readonly HttpClient _httpClient;

    public ReportServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ReportByAnalysisResponse> GetReportByAnalysisAsync(
        Guid analysisId,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"/api/report/by-analysis/{analysisId}", cancellationToken);
        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        return await ReadFromJsonAsync<ReportByAnalysisResponse>(response, cancellationToken);
    }

    public async Task<ReportByAnalysisResponse?> TryGetReportByAnalysisAsync(
        Guid analysisId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await GetReportByAnalysisAsync(analysisId, cancellationToken);
        }
        catch (UpstreamServiceException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ServiceFileResponse> DownloadReportByAnalysisAsync(
        Guid analysisId,
        string format,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"/api/report/by-analysis/{analysisId}/files/{Uri.EscapeDataString(format)}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');

        return new ServiceFileResponse(content, contentType, fileName);
    }

    private static async Task<T> ReadFromJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonDefaults.Options, cancellationToken);

        if (value is null)
        {
            throw new InvalidOperationException("The upstream service returned an empty response body.");
        }

        return value;
    }
}
