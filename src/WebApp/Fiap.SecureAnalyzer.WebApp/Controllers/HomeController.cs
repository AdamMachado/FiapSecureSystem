using Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway;
using Fiap.SecureAnalyzer.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Fiap.SecureAnalyzer.WebApp.Controllers;

public class HomeController(IApiGatewayClient apiGatewayClient) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        try
        {
            var analyses = await apiGatewayClient.ListAnalysesAsync(pageNumber: 1, pageSize: 20, cancellationToken);
            var items = analyses.Items
                .Select(analysis => new AnalysisListItemViewModel
                {
                    Id = analysis.AnalysisRequestId,
                    FileName = analysis.FileName,
                    ContentType = analysis.ContentType,
                    CreatedAtUtc = analysis.CreatedAtUtc,
                    Status = StatusPresentation.ToDisplayLabel(analysis.Status),
                    StatusCssClass = StatusPresentation.ToCssClass(analysis.Status),
                    HasReport = StatusPresentation.HasReport(analysis.Status)
                })
                .ToArray();

            var model = new DashboardViewModel
            {
                TotalAnalyses = analyses.TotalCount,
                ProcessingAnalyses = items.Count(item => item.StatusCssClass == "processing"),
                CompletedAnalyses = items.Count(item => item.StatusCssClass == "completed"),
                FailedAnalyses = items.Count(item => item.StatusCssClass == "error"),
                RecentAnalyses = items
            };

            return View(model);
        }
        catch (ApiGatewayClientException exception)
        {
            return View(new DashboardViewModel
            {
                ErrorMessage = exception.Message
            });
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
}
