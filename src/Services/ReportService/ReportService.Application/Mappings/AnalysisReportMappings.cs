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
}
