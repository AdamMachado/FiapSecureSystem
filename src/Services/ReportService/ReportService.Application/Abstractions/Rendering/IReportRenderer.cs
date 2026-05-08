using ReportService.Domain.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;

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
    AnalysisResultDto AnalysisResult);

public sealed record RenderedReport(
    string FileName,
    string ContentType,
    byte[] Content);
