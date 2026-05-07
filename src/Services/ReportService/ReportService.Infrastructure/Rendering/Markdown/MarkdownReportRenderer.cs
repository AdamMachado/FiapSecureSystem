using System.Text;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Domain.Enums;
using ReportService.Infrastructure.Exceptions;

namespace ReportService.Infrastructure.Rendering.Markdown;

public sealed class MarkdownReportRenderer : IReportRenderer
{
    public bool CanRender(ReportFormat format)
        => format == ReportFormat.Markdown;

    public Task<RenderedReport> RenderAsync(
        RenderReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {

            var contentBytes = Encoding.UTF8.GetBytes(request.Content);

            var fileName = $"{request.FileNameWithoutExtension}.md";

            return Task.FromResult(new RenderedReport(
                FileName: fileName,
                ContentType: "text/markdown",
                Content: contentBytes));
        }
        catch (Exception ex)
        {
            throw new ReportRenderingException("Failed to render report as Markdown.", ex);
        }
    }
}
