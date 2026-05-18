using System.Net;
using System.Text.Json;
using Fiap.SecureSystem.WebApp.Clients.ApiGateway;
using Fiap.SecureSystem.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.SecureSystem.WebApp.Controllers;

[Authorize]
public class ReportsController(IApiGatewayClient apiGatewayClient) : Controller
{
    private static readonly HashSet<string> AllowedReportFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "markdown",
        "json",
        "pdf"
    };

    public async Task<IActionResult> Details(Guid analysisId, CancellationToken cancellationToken)
    {
        try
        {
            var report = await apiGatewayClient.GetReportByAnalysisAsync(analysisId, cancellationToken);
            var components = MapComponents(report.AnalysisData);
            var risks = MapRisks(report.AnalysisData);
            var recommendations = MapRecommendations(report.AnalysisData);

            var model = new ReportDetailsViewModel
            {
                AnalysisId = report.AnalysisRequestId,
                ReportId = report.ReportId,
                CreatedAtUtc = report.CreatedAtUtc,
                UpdatedAtUtc = report.UpdatedAtUtc,
                ComponentCount = components.Count,
                RiskCount = risks.Count,
                RecommendationCount = recommendations.Count,
                SummaryOverview = TryGetSummaryOverview(report.AnalysisData),
                RequiresManualReview = TryGetSummaryBool(report.AnalysisData, "requiresManualReview"),
                Warnings = TryGetSummaryArray(report.AnalysisData, "warnings"),
                Components = components,
                Risks = risks,
                Recommendations = recommendations,
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
                    .ToArray(),
                AssetDownloadUrl = Url.Action("DownloadAsset", "Analyses", new { id = analysisId })
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
        if (!AllowedReportFormats.Contains(format))
        {
            return BadRequest("Formato de relatorio invalido.");
        }

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

    private static IReadOnlyCollection<ReportComponentViewModel> MapComponents(JsonElement root)
        => TryGetArray(root, "components", "identifiedComponents")
            .Select(component => new ReportComponentViewModel
            {
                Id = GetString(component, "id"),
                Name = GetString(component, "name", fallback: "Componente sem nome"),
                Type = GetString(component, "type"),
                Description = GetString(component, "description", fallback: "Sem descricao informada."),
                Tags = GetStringArray(component, "tags"),
                ConnectedTo = GetStringArray(component, "connectedTo"),
                Metadata = GetStringDictionary(component, "metadata")
            })
            .ToArray();

    private static IReadOnlyCollection<ReportRiskViewModel> MapRisks(JsonElement root)
        => TryGetArray(root, "risks", "architecturalRisks")
            .Select(risk =>
            {
                var severity = GetString(risk, "severity", fallback: "Medium");
                return new ReportRiskViewModel
                {
                    Id = GetString(risk, "id"),
                    Title = GetString(risk, "title", fallback: "Risco sem titulo"),
                    Description = GetString(risk, "description", fallback: "Sem descricao informada."),
                    Severity = severity,
                    SeverityCssClass = ToLevelCssClass(severity),
                    AffectedComponentId = GetString(risk, "affectedComponentId"),
                    AffectedComponentName = GetString(risk, "affectedComponentName"),
                    Impact = GetString(risk, "impact", fallback: "Nao informado."),
                    Likelihood = GetString(risk, "likelihood", fallback: "Nao informado."),
                    Evidence = GetStringArray(risk, "evidence")
                };
            })
            .ToArray();

    private static IReadOnlyCollection<ReportRecommendationViewModel> MapRecommendations(JsonElement root)
        => TryGetArray(root, "recommendations", "suggestions")
            .Select(recommendation =>
            {
                var priority = GetString(recommendation, "priority", fallback: "Medium");
                return new ReportRecommendationViewModel
                {
                    Id = GetString(recommendation, "id"),
                    Title = GetString(recommendation, "title", fallback: "Recomendacao sem titulo"),
                    Description = GetString(recommendation, "description", fallback: "Sem descricao informada."),
                    Category = GetString(recommendation, "category"),
                    Priority = priority,
                    PriorityCssClass = ToLevelCssClass(priority),
                    RelatedRiskId = GetString(recommendation, "relatedRiskId"),
                    TargetComponentId = GetString(recommendation, "targetComponentId"),
                    ExpectedBenefits = GetStringArray(recommendation, "expectedBenefits")
                };
            })
            .ToArray();

    private static string TryGetSummaryOverview(JsonElement root)
        => TryGetObject(root, "summary", out var summary)
            ? GetString(summary, "overview", fallback: "Resumo nao informado.")
            : "Resumo nao informado.";

    private static bool TryGetSummaryBool(JsonElement root, string propertyName)
        => TryGetObject(root, "summary", out var summary)
            && summary.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.True;

    private static IReadOnlyCollection<string> TryGetSummaryArray(JsonElement root, string propertyName)
        => TryGetObject(root, "summary", out var summary)
            ? GetStringArray(summary, propertyName)
            : Array.Empty<string>();

    private static IReadOnlyCollection<JsonElement> TryGetArray(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(propertyName, out var element)
                && element.ValueKind == JsonValueKind.Array)
            {
                return element.EnumerateArray().ToArray();
            }
        }

        return Array.Empty<JsonElement>();
    }

    private static bool TryGetObject(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(propertyName, out value)
            && value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string GetString(JsonElement element, string propertyName, string fallback = "")
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return fallback;

        return property.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(property.GetString()) ? fallback : property.GetString()!,
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.GetRawText(),
            _ => fallback
        };
    }

    private static IReadOnlyCollection<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return property
            .EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> GetStringDictionary(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>();
        }

        return property
            .EnumerateObject()
            .ToDictionary(
                item => item.Name,
                item => item.Value.ValueKind == JsonValueKind.String
                    ? item.Value.GetString() ?? string.Empty
                    : item.Value.GetRawText());
    }

    private static string ToLevelCssClass(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Contains("critical") || normalized.Contains("high") || normalized.Contains("alta"))
            return "high";

        if (normalized.Contains("low") || normalized.Contains("baixa"))
            return "low";

        return "medium";
    }
}
