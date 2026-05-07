using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.DownloadReportFile;

public sealed record DownloadReportFileQuery(
    Guid AnalysisRequestId,
    ReportFormat Format);
