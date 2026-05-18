using System.Net;
using Fiap.SecureSystem.WebApp.Clients.ApiGateway;
using Fiap.SecureSystem.WebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.SecureSystem.WebApp.Controllers;

public class AnalysesController(IApiGatewayClient apiGatewayClient) : Controller
{
    private static readonly HashSet<string> AllowedReportFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "markdown",
        "json",
        "pdf"
    };

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await apiGatewayClient.GetAnalysisDetailsAsync(id, cancellationToken);
            return View(MapAnalysisDetails(response));
        }
        catch (ApiGatewayClientException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> DetailsStatus(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await apiGatewayClient.GetAnalysisDetailsAsync(id, cancellationToken);
            return Json(MapAnalysisDetails(response));
        }
        catch (ApiGatewayClientException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound();
        }
    }

    public IActionResult Create()
    {
        return View(new AnalysisCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return View("Create", new AnalysisCreateViewModel
            {
                ErrorMessage = "Selecione um arquivo antes de enviar."
            });
        }

        try
        {
            var response = await apiGatewayClient.UploadAnalysisAsync(file, cancellationToken);
            return RedirectToAction(nameof(Details), new { id = response.AnalysisRequestId });
        }
        catch (ApiGatewayClientException exception)
        {
            return View("Create", new AnalysisCreateViewModel
            {
                ErrorMessage = exception.Message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadReport(Guid id, string format = "pdf", CancellationToken cancellationToken = default)
    {
        if (!AllowedReportFormats.Contains(format))
        {
            return BadRequest("Formato de relatorio invalido.");
        }

        try
        {
            var file = await apiGatewayClient.DownloadReportAsync(id, format, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (ApiGatewayClientException exception)
        {
            TempData["DownloadReportError"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAsset(Guid id, CancellationToken cancellationToken)
    {
        var file = await apiGatewayClient.DownloadAnalysisAssetAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private AnalysisDetailsViewModel MapAnalysisDetails(Clients.ApiGateway.Contracts.AnalysisDetailsResponse response)
    {
        var analysis = response.Analysis;
        var normalizedStatus = analysis.Status?.Trim().ToLowerInvariant() ?? string.Empty;

        return new AnalysisDetailsViewModel
        {
            Id = analysis.AnalysisRequestId,
            FileName = analysis.FileName,
            StatusCode = normalizedStatus,
            Status = StatusPresentation.ToDisplayLabel(analysis.Status),
            StatusCssClass = StatusPresentation.ToCssClass(analysis.Status),
            ContentType = analysis.ContentType,
            SizeInBytes = analysis.SizeInBytes,
            CreatedAtUtc = analysis.CreatedAtUtc,
            UpdatedAtUtc = analysis.UpdatedAtUtc,
            StartedAtUtc = analysis.StartedAtUtc,
            CompletedAtUtc = analysis.CompletedAtUtc,
            FailedAtUtc = analysis.FailedAtUtc,
            FailureReason = analysis.FailureReason,
            HasReport = response.Report is not null,
            ReportDetailsUrl = response.Report is null ? null : Url.Action("Details", "Reports", new { analysisId = analysis.AnalysisRequestId }),
            ReportDownloadUrl = response.Report is null ? null : Url.Action(nameof(DownloadReport), new { id = analysis.AnalysisRequestId }),
            AssetDownloadUrl = Url.Action(nameof(DownloadAsset), new { id = analysis.AnalysisRequestId })
        };
    }
}
