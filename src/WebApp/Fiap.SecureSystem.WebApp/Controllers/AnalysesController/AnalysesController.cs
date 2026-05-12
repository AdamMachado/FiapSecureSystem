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
            var model = new AnalysisDetailsViewModel
            {
                Id = response.Analysis.AnalysisRequestId,
                FileName = response.Analysis.FileName,
                Status = StatusPresentation.ToDisplayLabel(response.Analysis.Status),
                StatusCssClass = StatusPresentation.ToCssClass(response.Analysis.Status),
                ContentType = response.Analysis.ContentType,
                SizeInBytes = response.Analysis.SizeInBytes,
                CreatedAtUtc = response.Analysis.CreatedAtUtc,
                UpdatedAtUtc = response.Analysis.UpdatedAtUtc,
                StartedAtUtc = response.Analysis.StartedAtUtc,
                CompletedAtUtc = response.Analysis.CompletedAtUtc,
                FailedAtUtc = response.Analysis.FailedAtUtc,
                FailureReason = response.Analysis.FailureReason,
                HasReport = response.Report is not null,
                ReportDetailsUrl = response.Report is null ? null : Url.Action("Details", "Reports", new { analysisId = id }),
                ReportDownloadUrl = response.Report is null ? null : Url.Action(nameof(DownloadReport), new { id }),
                AssetDownloadUrl = Url.Action(nameof(DownloadAsset), new { id })
            };

            return View(model);
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
}
