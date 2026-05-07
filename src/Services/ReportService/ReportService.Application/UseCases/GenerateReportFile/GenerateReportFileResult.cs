using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.GenerateReportFile;

public sealed record GenerateReportFileResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    ReportFormat Format,
    string FileName,
    string ContentType,
    string BucketName,
    string ObjectKey,
    DateTime GeneratedAtUtc);
