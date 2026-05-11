using Microsoft.AspNetCore.Mvc;
using Shared.Kernel.Pagination;
using System.Security.Cryptography;
using UploadService.Api.Configuration;
using UploadService.Api.Contracts.Requests;
using UploadService.Api.Contracts.Responses;
using UploadService.Application.UseCases.Common;
using UploadService.Application.UseCases.CreateAnalysis;
using UploadService.Application.UseCases.GetAnalysisAsset;
using UploadService.Application.UseCases.GetAnalysisRequestsByIds;
using UploadService.Application.UseCases.GetAnalysisStatus;
using UploadService.Application.UseCases.ListUserAnalysisRequests;

namespace UploadService.Api.Controllers;

[ApiController]
[Route("api/analysis")]
[Produces("application/json")]
public sealed class AnalysesController : ControllerBase
{
    private readonly ILogger<AnalysesController> _logger;

    public AnalysesController(ILogger<AnalysesController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateAnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> CreateAsync(
        [FromForm] CreateAnalysisRequest request,
        [FromServices] CreateAnalysisHandler handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to create analysis for file: {FileName}", request.File?.FileName);

        if (request.File is null)
        {
            _logger.LogWarning("No file provided in the request.");

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.File)] = ["A file is required."]
            });
        }

        await using var fileStream = request.File.OpenReadStream();
        var fileHash = await ComputeSha256Async(fileStream, cancellationToken);
        fileStream.Position = 0;

        var command = new CreateAnalysisCommand(
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            fileStream,
            fileHash);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = new CreateAnalysisResponse(
            result.Value.AnalysisRequestId,
            result.Value.Status.ToString(),
            result.Value.CreatedAtUtc,
            Url.ActionLink(
                action: nameof(GetStatusAsync),
                controller: "Analyses",
                values: new { analysisRequestId = result.Value.AnalysisRequestId }) ??
            $"/api/analyses/{result.Value.AnalysisRequestId}");

        return Results.Created($"/api/analyses/{result.Value.AnalysisRequestId}", response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnalysisSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> ListAsync(
        [FromQuery] PaginationParams paginationParams,
        [FromServices] ListUserAnalysisRequestsHandler handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received request to list analyses. PageNumber: {PageNumber}, PageSize: {PageSize}",
            paginationParams.PageNumber,
            paginationParams.PageSize);

        var result = await handler.HandleAsync(
            new ListUserAnalysisRequestsQuery(paginationParams),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = PagedResult<AnalysisSummaryResponse>.Create(
            result.Value.Items.Select(MapAnalysisSummaryResponse).ToArray(),
            result.Value.TotalCount,
            result.Value.PageNumber,
            result.Value.PageSize);

        return Results.Ok(response);
    }

    [HttpPost("by-ids")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AnalysisSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> ListByIdsAsync(
        [FromBody] GetAnalysisRequestsByIdsRequest request,
        [FromServices] GetAnalysisRequestsByIdsHandler handler,
        CancellationToken cancellationToken)
    {
        var analysisRequestIds = request?.AnalysisRequestIds ?? Array.Empty<Guid>();

        _logger.LogInformation(
            "Received request to list analyses by ids. RequestedCount: {RequestedCount}",
            analysisRequestIds.Count);

        var result = await handler.HandleAsync(
            new GetAnalysisRequestsByIdsQuery(analysisRequestIds),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = result.Value
            .Select(MapAnalysisSummaryResponse)
            .ToArray();

        return Results.Ok(response);
    }

    [HttpGet("{analysisRequestId:guid}")]
    [ProducesResponseType(typeof(GetAnalysisStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> GetStatusAsync(
        Guid analysisRequestId,
        [FromServices] GetAnalysisStatusHandler handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get analysis status for AnalysisRequestId: {AnalysisRequestId}", analysisRequestId);

        var result = await handler.HandleAsync(new GetAnalysisStatusQuery(analysisRequestId), cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        var response = new GetAnalysisStatusResponse(
            result.Value.AnalysisRequestId,
            result.Value.Status.ToString(),
            result.Value.FileName,
            result.Value.ContentType,
            result.Value.SizeInBytes,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc,
            result.Value.StartedAtUtc,
            result.Value.CompletedAtUtc,
            result.Value.FailedAtUtc,
            result.Value.FailureReason);

        return Results.Ok(response);
    }

    [HttpGet("{analysisRequestId:guid}/asset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IResult> GetAssetAsync(
        Guid analysisRequestId,
        [FromServices] GetAnalysisAssetHandler handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received request to download analysis asset for AnalysisRequestId: {AnalysisRequestId}",
            analysisRequestId);

        var result = await handler.HandleAsync(
            new GetAnalysisAssetQuery(analysisRequestId),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblemHttpResult();

        return Results.File(result.Value.Content, result.Value.ContentType);
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static AnalysisSummaryResponse MapAnalysisSummaryResponse(AnalysisRequestSummaryResult summary)
        => new(
            summary.AnalysisRequestId,
            summary.Status.ToString(),
            summary.FileName,
            summary.ContentType,
            summary.SizeInBytes,
            summary.CreatedAtUtc,
            summary.UpdatedAtUtc,
            summary.StartedAtUtc,
            summary.CompletedAtUtc,
            summary.FailedAtUtc,
            summary.FailureReason);
}
