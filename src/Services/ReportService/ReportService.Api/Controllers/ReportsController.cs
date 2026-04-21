using Microsoft.AspNetCore.Mvc;
using ReportService.Api.Contracts.Requests;
using ReportService.Api.Contracts.Responses;
using ReportService.Application.UseCases.DownloadReport;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Application.UseCases.GetReportByAnalysis;

namespace ReportService.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(GenerateReportResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateReportRequest request,
        [FromServices] GenerateReportHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new GenerateReportCommand(
            request.AnalysisRequestId,
            request.RequestedByUserId,
            request.Result,
            request.Format);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(result.Error);

        var response = new GenerateReportResponse(
            result.Value.ReportId,
            result.Value.AnalysisRequestId,
            result.Value.Format,
            result.Value.Status,
            result.Value.FileName,
            result.Value.GeneratedAtUtc);

        return CreatedAtAction(
            nameof(GetByAnalysis),
            new { analysisRequestId = response.AnalysisRequestId },
            response);
    }

    [HttpGet("analysis/{analysisRequestId:guid}")]
    [ProducesResponseType(typeof(GetReportByAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAnalysis(
        [FromRoute] Guid analysisRequestId,
        [FromServices] GetReportByAnalysisHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetReportByAnalysisQuery(analysisRequestId);
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
            return NotFound(result.Error);

        var response = new GetReportByAnalysisResponse(
            result.Value.ReportId,
            result.Value.AnalysisRequestId,
            result.Value.RequestedByUserId,
            result.Value.Format,
            result.Value.Status,
            result.Value.FileName,
            result.Value.ContentType,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc,
            result.Value.GeneratedAtUtc,
            result.Value.FailureReason);

        return Ok(response);
    }

    [HttpGet("{reportId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        [FromRoute] Guid reportId,
        [FromServices] DownloadReportHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new DownloadReportQuery(reportId);
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
            return NotFound(result.Error);

        return File(
            result.Value.Content,
            result.Value.ContentType,
            result.Value.FileName);
    }
}