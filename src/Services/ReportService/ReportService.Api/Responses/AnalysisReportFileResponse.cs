using ReportService.Domain.Enums;

namespace ReportService.Api.Contracts.Responses;

public sealed record AnalysisReportFileResponse(
    ReportFormat Format,
    string FileName,
    string ContentType,
    string BucketName,
    string ObjectKey,
    DateTime GeneratedAtUtc);
