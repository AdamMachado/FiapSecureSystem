using System.Net;
using System.Text.Json;
using Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway;
using Fiap.SecureAnalyzer.WebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.SecureAnalyzer.WebApp.Controllers;

public class ReportsController(IApiGatewayClient apiGatewayClient) : Controller
{
    public async Task<IActionResult> Details(Guid analysisId, CancellationToken cancellationToken)
    {
        try
        {
            var report = await apiGatewayClient.GetReportByAnalysisAsync(analysisId, cancellationToken);
            var json = JsonSerializer.Serialize(report.AnalysisData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var model = new ReportDetailsViewModel
            {
                AnalysisId = report.AnalysisRequestId,
                ReportId = report.ReportId,
                CreatedAtUtc = report.CreatedAtUtc,
                UpdatedAtUtc = report.UpdatedAtUtc,
                ComponentCount = TryCount(report.AnalysisData, "components", "identifiedComponents"),
                RiskCount = TryCount(report.AnalysisData, "risks", "architecturalRisks"),
                RecommendationCount = TryCount(report.AnalysisData, "recommendations", "suggestions"),
                AnalysisDataJson = json,
                Files = report.Files
                    .Select(file => new ReportFileViewModel
                    {
                        Format = file.Format.ToString().ToUpperInvariant(),
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        GeneratedAtUtc = file.GeneratedAtUtc,
                        DownloadUrl = Url.Action(nameof(Download), new
                        {
                            analysisId,
                            format = file.Format.ToString().ToLowerInvariant()
                        }) ?? string.Empty
                    })
                    .ToArray()
            };

            return View(model);
        }
        catch (ApiGatewayClientException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return View(new ReportDetailsViewModel
            {
                AnalysisId = analysisId,
                ErrorMessage = "O relatório ainda não está disponível para esta análise."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid analysisId, string format = "pdf", CancellationToken cancellationToken = default)
    {
        var file = await apiGatewayClient.DownloadReportAsync(analysisId, format, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private static int TryCount(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(propertyName, out var element)
                && element.ValueKind == JsonValueKind.Array)
            {
                return element.GetArrayLength();
            }
        }

        return 0;
    }
}
