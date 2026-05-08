using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.DownloadReportFile;

public sealed record DownloadReportFileResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    ReportFormat Format,
    string FileName,
    string ContentType,
    Stream Content);
