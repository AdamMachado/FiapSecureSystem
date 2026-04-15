namespace ReportService.Application.UseCases.DownloadReport;

public sealed record DownloadReportResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    string FileName,
    string ContentType,
    Stream Content);