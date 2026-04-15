using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.GetReportByAnalysis;

public sealed record GetReportByAnalysisResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    ReportStatus Status,
    ReportFormat Format,
    string FileName,
    string ContentType,
    string BucketName,
    string ObjectKey,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? GeneratedAtUtc,
    string? FailureReason);