using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Domain.Enums;
using ReportService.Infrastructure.Exceptions;

namespace ReportService.Infrastructure.Rendering.Json;

public sealed class JsonReportRenderer : IReportRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static JsonReportRenderer()
    {
        SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public bool CanRender(ReportFormat format)
        => format == ReportFormat.Json;

    public Task<RenderedReport> RenderAsync(
        RenderReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request.AnalysisResult, SerializerOptions);
            var contentBytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"{request.FileNameWithoutExtension}.json";

            return Task.FromResult(new RenderedReport(
                FileName: fileName,
                ContentType: "application/json",
                Content: contentBytes));
        }
        catch (Exception ex)
        {
            throw new ReportRenderingException("Failed to render report as JSON.", ex);
        }
    }
}
