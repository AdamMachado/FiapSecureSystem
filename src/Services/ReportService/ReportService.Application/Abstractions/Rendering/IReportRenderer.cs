using ReportService.Domain.Enums;

namespace ReportService.Application.Abstractions.Rendering;

public interface IReportRenderer
{
    bool CanRender(ReportFormat format);

    Task<RenderedReport> RenderAsync(
        RenderReportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record RenderReportRequest(
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    ReportFormat Format,
    string FileNameWithoutExtension,
    string Content);

public sealed record RenderedReport(
    string FileName,
    string ContentType,
    byte[] Content);
