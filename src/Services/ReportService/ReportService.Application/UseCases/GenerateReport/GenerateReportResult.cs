using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.GenerateReport;

public sealed record GenerateReportResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    ReportStatus Status,
    ReportFormat Format,
    string FileName,
    string BucketName,
    string ObjectKey,
    DateTime GeneratedAtUtc);