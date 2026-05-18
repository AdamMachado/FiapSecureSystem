using Fiap.SecureSystem.ApiGateway.Contracts.Responses;
using Fiap.SecureSystem.ApiGateway.Services.Common;
using Fiap.SecureSystem.ApiGateway.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Security.Authorization;

namespace Fiap.SecureSystem.ApiGateway.Controllers;

[ApiController]
[Route("api/report")]
[Produces("application/json")]
public sealed class ReportController : ControllerBase
{
    [HttpGet("by-analysis/{analysisId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ReportRead)]
    [ProducesResponseType(typeof(ReportByAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetReportByAnalysisAsync(
        [FromRoute] Guid analysisId,
        [FromServices] IReportServiceClient reportServiceClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await reportServiceClient.GetReportByAnalysisAsync(analysisId, cancellationToken);
            return Ok(response);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    [HttpGet("by-analysis/{analysisId:guid}/files/{format}")]
    [Authorize(Policy = AuthorizationPolicies.ReportRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DownloadReportByTypeAsync(
        [FromRoute] Guid analysisId,
        [FromRoute] string format,
        [FromServices] IReportServiceClient reportServiceClient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            ModelState.AddModelError(nameof(format), "The report format is required.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var file = await reportServiceClient.DownloadReportByAnalysisAsync(analysisId, format, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (UpstreamServiceException exception)
        {
            return ToProblemResult(exception);
        }
    }

    private IActionResult ToProblemResult(UpstreamServiceException exception)
    {
        var statusCode = exception.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => StatusCodes.Status400BadRequest,
            System.Net.HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status502BadGateway
        };

        return Problem(
            statusCode: statusCode,
            title: statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status404NotFound => "Not Found",
                _ => "Bad Gateway"
            },
            detail: exception.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = exception.Code,
                ["service"] = exception.ServiceName
            });
    }
}
