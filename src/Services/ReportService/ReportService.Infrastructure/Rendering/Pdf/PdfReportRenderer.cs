using System.Text;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Domain.Enums;
using ReportService.Infrastructure.Exceptions;

namespace ReportService.Infrastructure.Rendering.Pdf;

public sealed class PdfReportRenderer : IReportRenderer
{
    public bool CanRender(ReportFormat format)
        => format == ReportFormat.Pdf;

    public Task<RenderedReport> RenderAsync(
        RenderReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var text = $"""

                {request.Content}
                """;

            var contentBytes = Encoding.UTF8.GetBytes(text);
            var fileName = $"{request.FileNameWithoutExtension}.pdf";

            return Task.FromResult(new RenderedReport(
                FileName: fileName,
                ContentType: "application/pdf",
                Content: contentBytes));
        }
        catch (Exception ex)
        {
            throw new ReportRenderingException("Failed to render report as PDF.", ex);
        }
    }
}
