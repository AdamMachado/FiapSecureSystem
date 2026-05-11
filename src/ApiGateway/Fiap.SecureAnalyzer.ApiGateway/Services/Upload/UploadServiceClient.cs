using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Fiap.SecureAnalyzer.ApiGateway.Contracts.Requests;
using Fiap.SecureAnalyzer.ApiGateway.Contracts.Responses;
using Fiap.SecureAnalyzer.ApiGateway.Serialization;
using Fiap.SecureAnalyzer.ApiGateway.Services.Common;
using Shared.Kernel.Pagination;

namespace Fiap.SecureAnalyzer.ApiGateway.Services.Upload;

public sealed class UploadServiceClient : IUploadServiceClient
{
    private const string ServiceName = "UploadService.Api";
    private readonly HttpClient _httpClient;

    public UploadServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CreateAnalysisResponse> UploadAnalysisAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        await using var fileStream = file.OpenReadStream();
        using var streamContent = new StreamContent(fileStream);

        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        }

        form.Add(streamContent, "file", file.FileName);

        using var response = await _httpClient.PostAsync("/api/analysis", form, cancellationToken);
        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        return await ReadFromJsonAsync<CreateAnalysisResponse>(response, cancellationToken);
    }

    public async Task<PagedResult<AnalysisSummaryResponse>> ListAnalysesAsync(
        PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        var path = $"/api/analysis?pageNumber={paginationParams.PageNumber}&pageSize={paginationParams.PageSize}";

        using var response = await _httpClient.GetAsync(path, cancellationToken);
        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        return await ReadFromJsonAsync<PagedResult<AnalysisSummaryResponse>>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AnalysisSummaryResponse>> GetAnalysesByIdsAsync(
        IReadOnlyCollection<Guid> analysisRequestIds,
        CancellationToken cancellationToken)
    {
        var request = new AnalysisIdsRequest(analysisRequestIds);
        using var content = CreateJsonContent(request);
        using var response = await _httpClient.PostAsync("/api/analysis/by-ids", content, cancellationToken);

        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        return await ReadFromJsonAsync<IReadOnlyCollection<AnalysisSummaryResponse>>(response, cancellationToken);
    }

    public async Task<AnalysisStatusResponse> GetAnalysisAsync(
        Guid analysisRequestId,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"/api/analysis/{analysisRequestId}", cancellationToken);
        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        return await ReadFromJsonAsync<AnalysisStatusResponse>(response, cancellationToken);
    }

    public async Task<ServiceFileResponse> DownloadAnalysisAssetAsync(
        Guid analysisRequestId,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"/api/analysis/{analysisRequestId}/asset",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await UpstreamServiceException.ThrowIfUnsuccessfulAsync(response, ServiceName, cancellationToken);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"');

        return new ServiceFileResponse(content, contentType, fileName);
    }

    private static StringContent CreateJsonContent<T>(T payload)
        => new(JsonSerializer.Serialize(payload, JsonDefaults.Options), Encoding.UTF8, "application/json");

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
