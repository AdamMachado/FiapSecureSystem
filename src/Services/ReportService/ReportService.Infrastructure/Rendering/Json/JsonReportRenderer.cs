using System.Text;
using System.Text.Json;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Domain.Enums;
using ReportService.Infrastructure.Exceptions;

namespace ReportService.Infrastructure.Rendering.Json;

public sealed class JsonReportRenderer : IReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public bool CanRender(ReportFormat format)
        => format == ReportFormat.Json;

    public Task<RenderedReport> RenderAsync(
        RenderReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                request.AnalysisRequestId,
                request.RequestedByUserId,
                request.Content
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);
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