using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;

namespace Fiap.SecureSystem.WebApp.Clients.ApiGateway;

public sealed class ApiGatewayClient(HttpClient httpClient) : IApiGatewayClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    static ApiGatewayClient()
    {
        SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<PagedResult<AnalysisSummaryResponse>> ListAnalysesAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync(
            $"/api/analysis?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken),
            cancellationToken);

        return await ReadJsonAsync<PagedResult<AnalysisSummaryResponse>>(response, cancellationToken)
            ?? new PagedResult<AnalysisSummaryResponse>();
    }

    public async Task<IReadOnlyCollection<AnalysisSummaryResponse>> CheckPendingAnalysisStatusAsync(
        IReadOnlyCollection<Guid> analysisIds,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => httpClient.PostAsJsonAsync(
                "/api/analysis/status-check",
                new AnalysisIdsRequest
                {
                    AnalysisRequestIds = analysisIds
                },
                SerializerOptions,
                cancellationToken),
            cancellationToken);

        return await ReadJsonAsync<IReadOnlyCollection<AnalysisSummaryResponse>>(response, cancellationToken)
            ?? Array.Empty<AnalysisSummaryResponse>();
    }

    public async Task<AnalysisDetailsResponse> GetAnalysisDetailsAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync($"/api/analysis/{analysisId}", cancellationToken),
            cancellationToken);
        return await ReadJsonAsync<AnalysisDetailsResponse>(response, cancellationToken)
            ?? throw new ApiGatewayClientException(HttpStatusCode.BadGateway, "ApiGateway returned an empty analysis response.");
    }

    public async Task<CreateAnalysisResponse> UploadAnalysisAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var formData = new MultipartFormDataContent();
        await using var fileStream = file.OpenReadStream();
        using var fileContent = new StreamContent(fileStream);

        fileContent.Headers.ContentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? new MediaTypeHeaderValue("application/octet-stream")
            : MediaTypeHeaderValue.Parse(file.ContentType);

        formData.Add(fileContent, "file", file.FileName);

        using var response = await SendAsync(
            () => httpClient.PostAsync("/api/analysis", formData, cancellationToken),
            cancellationToken);
        return await ReadJsonAsync<CreateAnalysisResponse>(response, cancellationToken)
            ?? throw new ApiGatewayClientException(HttpStatusCode.BadGateway, "ApiGateway returned an empty upload response.");
    }

    public async Task<ReportByAnalysisResponse> GetReportByAnalysisAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync($"/api/report/by-analysis/{analysisId}", cancellationToken),
            cancellationToken);
        return await ReadJsonAsync<ReportByAnalysisResponse>(response, cancellationToken)
            ?? throw new ApiGatewayClientException(HttpStatusCode.BadGateway, "ApiGateway returned an empty report response.");
    }

    public async Task<ApiGatewayFileResponse> DownloadReportAsync(Guid analysisId, string format, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync($"/api/report/by-analysis/{analysisId}/files/{format}", cancellationToken),
            cancellationToken);
        return await ReadFileAsync(response, cancellationToken);
    }

    public async Task<ApiGatewayFileResponse> DownloadAnalysisAssetAsync(Guid analysisId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync($"/api/analysis/{analysisId}/asset", cancellationToken),
            cancellationToken);
        return await ReadFileAsync(response, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            return await send();
        }
        catch (HttpRequestException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiGatewayClientException(
                HttpStatusCode.BadGateway,
                $"Nao foi possivel conectar ao ApiGateway. Verifique se o servico esta em execucao. Detalhes: {exception.Message}");
        }
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        return await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
    }

    private static async Task<ApiGatewayFileResponse> ReadFileAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? "download.bin";

        return new ApiGatewayFileResponse(content, contentType, fileName.Trim('"'));
    }

    private static async Task<ApiGatewayClientException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var message = $"ApiGateway request failed with status code {(int)response.StatusCode}.";

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiGatewayProblemDetails>(SerializerOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                message = problem.Detail;
            }
        }
        catch
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                message = body;
            }
        }

        return new ApiGatewayClientException(response.StatusCode, message);
    }
}
