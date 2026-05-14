using Fiap.SecureSystem.ApiGateway.Contracts.Requests;
using Fiap.SecureSystem.ApiGateway.Contracts.Responses;
using Fiap.SecureSystem.ApiGateway.Services.Common;
using Fiap.SecureSystem.ApiGateway.Services.Report;
using Fiap.SecureSystem.ApiGateway.Services.Upload;
using Microsoft.AspNetCore.Mvc;
using Shared.Kernel.Pagination;

namespace Fiap.SecureSystem.ApiGateway.Controllers;

[ApiController]
[Route("api/analysis")]
[Produces("application/json")]
public sealed class AnalysisController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateAnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UploadDiagramAsync(
        [FromForm] UploadAnalysisRequest request,
        [FromServices] IUploadServiceClient uploadServiceClient,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            ModelState.AddModelError(nameof(request.File), "A file is required.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var upstreamResponse = await uploadServiceClient.UploadAnalysisAsync(request.File, cancellationToken);
            var statusUrl =
                Url.ActionLink(nameof(GetAnalysisDetailsAsync), values: new { analysisId = upstreamResponse.AnalysisRequestId })
                ?? $"/api/upload/analyses/{upstreamResponse.AnalysisRequestId}";

            var response = upstreamResponse with
            {
                StatusUrl = statusUrl
            };

            return Created(statusUrl, response);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnalysisSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ListAnalysesAsync(
        [FromQuery] PaginationParams paginationParams,
        [FromServices] IUploadServiceClient uploadServiceClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await uploadServiceClient.ListAnalysesAsync(paginationParams, cancellationToken);
            return Ok(response);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpPost("status-check")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AnalysisSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CheckPendingAnalysisStatusAsync(
        [FromBody] AnalysisIdsRequest request,
        [FromServices] IUploadServiceClient uploadServiceClient,
        CancellationToken cancellationToken)
    {
        var analysisRequestIds = request.AnalysisRequestIds ?? Array.Empty<Guid>();

        if (analysisRequestIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.AnalysisRequestIds), "At least one analysis id must be informed.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var response = await uploadServiceClient.GetAnalysesByIdsAsync(analysisRequestIds, cancellationToken);
            return Ok(response);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet("{analysisId:guid}")]
    [ProducesResponseType(typeof(AnalysisDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetAnalysisDetailsAsync(
        [FromRoute] Guid analysisId,
        [FromServices] IUploadServiceClient uploadServiceClient,
        [FromServices] IReportServiceClient reportServiceClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var analysisTask = uploadServiceClient.GetAnalysisAsync(analysisId, cancellationToken);
            var reportTask = reportServiceClient.TryGetReportByAnalysisAsync(analysisId, cancellationToken);

            await Task.WhenAll(analysisTask, reportTask);

            var response = new AnalysisDetailsResponse(
                await analysisTask,
                await reportTask);

            return Ok(response);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet("{analysisId:guid}/asset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DownloadAnalysisAssetAsync(
        [FromRoute] Guid analysisId,
        [FromServices] IUploadServiceClient uploadServiceClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await uploadServiceClient.DownloadAnalysisAssetAsync(analysisId, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private IActionResult ToProblemResult(UpstreamServiceException exception)
    {
        var statusCode = exception.StatusCode == System.Net.HttpStatusCode.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status502BadGateway;

        return Problem(
            statusCode: statusCode,
            title: statusCode == StatusCodes.Status404NotFound ? "Not Found" : "Bad Gateway",
            detail: exception.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = exception.Code,
                ["service"] = exception.ServiceName
            });
    }
}
