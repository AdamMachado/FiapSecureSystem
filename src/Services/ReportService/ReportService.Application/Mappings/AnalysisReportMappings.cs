using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ReportService.Application.Mappings;

public static class AnalysisReportMappings
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static string ToAnalysisJson(AnalysisResultDto result)
        => JsonSerializer.Serialize(result, JsonOptions);

    public static AnalysisResultDto FromAnalysisJson(string analysisJson)
    {
        var result = JsonSerializer.Deserialize<AnalysisResultDto>(analysisJson, JsonOptions);

        if (result is null)
            throw new InvalidOperationException("Analysis report data could not be deserialized.");

        return result;
    }

    public static JsonElement ToJsonElement(string analysisJson)
    {
        using var document = JsonDocument.Parse(analysisJson);
        return document.RootElement.Clone();
    }

    public static string ToMarkdownDocument(
        Guid analysisRequestId,
        Guid requestedByUserId,
        AnalysisResultDto result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Technical Architecture Analysis Report");
        sb.AppendLine();
        sb.AppendLine($"- AnalysisRequestId: `{analysisRequestId}`");
        sb.AppendLine($"- RequestedByUserId: `{requestedByUserId}`");
        sb.AppendLine();

        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine(result.Summary.Overview);
        sb.AppendLine();
        sb.AppendLine($"- Total Components: {result.Summary.TotalComponents}");
        sb.AppendLine($"- Total Risks: {result.Summary.TotalRisks}");
        sb.AppendLine($"- Total Recommendations: {result.Summary.TotalRecommendations}");
        sb.AppendLine($"- Requires Manual Review: {(result.Summary.RequiresManualReview ? "Yes" : "No")}");
        sb.AppendLine();

        if (result.Summary.Warnings.Count > 0)
        {
            sb.AppendLine("### Warnings");
            sb.AppendLine();

            foreach (var warning in result.Summary.Warnings)
            {
                sb.AppendLine($"- {warning}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("## Identified Components");
        sb.AppendLine();

        if (result.Components.Count == 0)
        {
            sb.AppendLine("_No components identified._");
            sb.AppendLine();
        }
        else
        {
            foreach (var component in result.Components)
            {
                sb.AppendLine($"### {component.Name}");
                sb.AppendLine();
                sb.AppendLine($"- Id: `{component.Id}`");
                sb.AppendLine($"- Type: {component.Type}");
                sb.AppendLine($"- Description: {component.Description ?? "N/A"}");

                if (component.Tags.Count > 0)
                    sb.AppendLine($"- Tags: {string.Join(", ", component.Tags)}");

                if (component.ConnectedTo.Count > 0)
                    sb.AppendLine($"- Connected To: {string.Join(", ", component.ConnectedTo)}");

                if (component.Metadata is { Count: > 0 })
                {
                    sb.AppendLine("- Metadata:");

                    foreach (var pair in component.Metadata)
                    {
                        sb.AppendLine($"  - {pair.Key}: {pair.Value}");
                    }
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine("## Architectural Risks");
        sb.AppendLine();

        if (result.Risks.Count == 0)
        {
            sb.AppendLine("_No architectural risks identified._");
            sb.AppendLine();
        }
        else
        {
            foreach (var risk in result.Risks)
            {
                sb.AppendLine($"### {risk.Title}");
                sb.AppendLine();
                sb.AppendLine($"- Id: `{risk.Id}`");
                sb.AppendLine($"- Severity: {risk.Severity}");
                sb.AppendLine($"- Description: {risk.Description}");
                sb.AppendLine($"- Affected Component Id: {risk.AffectedComponentId ?? "N/A"}");
                sb.AppendLine($"- Affected Component Name: {risk.AffectedComponentName ?? "N/A"}");
                sb.AppendLine($"- Impact: {risk.Impact ?? "N/A"}");
                sb.AppendLine($"- Likelihood: {risk.Likelihood ?? "N/A"}");

                if (risk.Evidence.Count > 0)
                {
                    sb.AppendLine("- Evidence:");

                    foreach (var evidence in risk.Evidence)
                    {
                        sb.AppendLine($"  - {evidence}");
                    }
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine("## Recommendations");
        sb.AppendLine();

        if (result.Recommendations.Count == 0)
        {
            sb.AppendLine("_No recommendations identified._");
            sb.AppendLine();
        }
        else
        {
            foreach (var recommendation in result.Recommendations)
            {
                sb.AppendLine($"### {recommendation.Title}");
                sb.AppendLine();
                sb.AppendLine($"- Id: `{recommendation.Id}`");
                sb.AppendLine($"- Category: {recommendation.Category}");
                sb.AppendLine($"- Priority: {recommendation.Priority}");
                sb.AppendLine($"- Description: {recommendation.Description}");
                sb.AppendLine($"- Related Risk Id: {recommendation.RelatedRiskId ?? "N/A"}");
                sb.AppendLine($"- Target Component Id: {recommendation.TargetComponentId ?? "N/A"}");

                if (recommendation.ExpectedBenefits.Count > 0)
                {
                    sb.AppendLine("- Expected Benefits:");

                    foreach (var benefit in recommendation.ExpectedBenefits)
                    {
                        sb.AppendLine($"  - {benefit}");
                    }
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
