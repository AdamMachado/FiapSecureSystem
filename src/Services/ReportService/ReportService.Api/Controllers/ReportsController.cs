using Microsoft.AspNetCore.Mvc;
using ReportService.Api.Contracts.Responses;
using ReportService.Application.UseCases.DownloadReportFile;
using ReportService.Application.UseCases.GetReportByAnalysis;
using ReportService.Domain.Enums;

namespace ReportService.Api.Controllers;

[ApiController]
[Route("reports")]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    [HttpGet("by-analysis/{analysisId:guid}")]
    [ProducesResponseType(typeof(GetReportByAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAnalysis(
        [FromRoute] Guid analysisId,
        [FromServices] GetReportByAnalysisHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetReportByAnalysisQuery(analysisId);
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
            return NotFound(result.Error);

        var response = new GetReportByAnalysisResponse(
            result.Value.ReportId,
            result.Value.AnalysisRequestId,
            result.Value.RequestedByUserId,
            result.Value.AnalysisData,
            result.Value.Files
                .Select(x => new AnalysisReportFileResponse(
                    x.Format,
                    x.FileName,
                    x.ContentType,
                    x.BucketName,
                    x.ObjectKey,
                    x.GeneratedAtUtc))
                .ToArray(),
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc);

        return Ok(response);
    }

    [HttpGet("by-analysis/{analysisId:guid}/files/{format}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadByAnalysis(
        [FromRoute] Guid analysisId,
        [FromRoute] string format,
        [FromServices] DownloadReportFileHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ReportFormat>(format, ignoreCase: true, out var parsedFormat))
        {
            return BadRequest(new
            {
                Code = "report.invalid_format",
                Message = $"Unsupported report format '{format}'."
            });
        }

        var query = new DownloadReportFileQuery(analysisId, parsedFormat);
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Type == Shared.Kernel.Result.ErrorType.NotFound)
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return File(
            result.Value.Content,
            result.Value.ContentType,
            result.Value.FileName);
    }
}
