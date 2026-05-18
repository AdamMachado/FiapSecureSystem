using Fiap.SecureSystem.WebApp.Clients.ApiGateway;
using Fiap.SecureSystem.WebApp.Clients.ApiGateway.Contracts;
using Fiap.SecureSystem.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Fiap.SecureSystem.WebApp.Controllers;

public class HomeController(IApiGatewayClient apiGatewayClient) : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        try
        {
            return View(await BuildDashboardViewModelAsync(cancellationToken));
        }
        catch (ApiGatewayClientException exception)
        {
            return View(new DashboardViewModel
            {
                ErrorMessage = exception.Message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DashboardAnalyses(CancellationToken cancellationToken)
    {
        try
        {
            return Json(await BuildDashboardViewModelAsync(cancellationToken));
        }
        catch (ApiGatewayClientException exception)
        {
            return Problem(
                statusCode: (int)exception.StatusCode,
                title: "ApiGateway error",
                detail: exception.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DashboardPendingStatuses([FromBody] AnalysisIdsRequest request, CancellationToken cancellationToken)
    {
        var analysisIds = request.AnalysisRequestIds
            .Where(analysisId => analysisId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (analysisIds.Length == 0)
        {
            return Json(Array.Empty<AnalysisListItemViewModel>());
        }

        try
        {
            var analyses = await apiGatewayClient.CheckPendingAnalysisStatusAsync(analysisIds, cancellationToken);
            var items = analyses.Select(MapAnalysis).ToArray();
            return Json(items);
        }
        catch (ApiGatewayClientException exception)
        {
            return Problem(
                statusCode: (int)exception.StatusCode,
                title: "ApiGateway error",
                detail: exception.Message);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private async Task<DashboardViewModel> BuildDashboardViewModelAsync(CancellationToken cancellationToken)
    {
        var analyses = await apiGatewayClient.ListAnalysesAsync(pageNumber: 1, pageSize: 20, cancellationToken);
        var items = analyses.Items.Select(MapAnalysis).ToArray();

        return new DashboardViewModel
        {
            TotalAnalyses = analyses.TotalCount,
            ProcessingAnalyses = items.Count(item => StatusPresentation.IsPendingOrProcessing(item.StatusCode)),
            CompletedAnalyses = items.Count(item => item.StatusCssClass == "completed"),
            FailedAnalyses = items.Count(item => item.StatusCssClass == "failed"),
            RecentAnalyses = items
        };
    }

    private AnalysisListItemViewModel MapAnalysis(AnalysisSummaryResponse analysis)
    {
        var normalizedStatus = NormalizeStatusCode(analysis.Status);

        return new AnalysisListItemViewModel
        {
            Id = analysis.AnalysisRequestId,
            FileName = analysis.FileName,
            ContentType = analysis.ContentType,
            CreatedAtUtc = analysis.CreatedAtUtc,
            UpdatedAtUtc = analysis.UpdatedAtUtc,
            CompletedAtUtc = analysis.CompletedAtUtc,
            FailedAtUtc = analysis.FailedAtUtc,
            StatusCode = normalizedStatus,
            Status = StatusPresentation.ToDisplayLabel(analysis.Status),
            StatusCssClass = StatusPresentation.ToCssClass(analysis.Status),
            HasReport = StatusPresentation.HasReport(analysis.Status),
            DetailUrl = Url.Action("Details", "Analyses", new { id = analysis.AnalysisRequestId }) ?? $"/Analyses/Details/{analysis.AnalysisRequestId}",
            ReportUrl = StatusPresentation.HasReport(analysis.Status)
                ? Url.Action("Details", "Reports", new { analysisId = analysis.AnalysisRequestId })
                : null
        };
    }

    private static string NormalizeStatusCode(string? status) =>
        status?.Trim().ToLowerInvariant() ?? string.Empty;
}
