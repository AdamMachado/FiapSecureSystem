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
            var markdown = new StringBuilder();

            markdown.AppendLine("# Technical Analysis Report");
            markdown.AppendLine();
            markdown.AppendLine($"**AnalysisRequestId:** `{request.AnalysisRequestId}`");
            markdown.AppendLine($"**RequestedByUserId:** `{request.RequestedByUserId}`");
            markdown.AppendLine();
            markdown.AppendLine("## Content");
            markdown.AppendLine();
            markdown.AppendLine(request.Content);

            var contentBytes = Encoding.UTF8.GetBytes(markdown.ToString());

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