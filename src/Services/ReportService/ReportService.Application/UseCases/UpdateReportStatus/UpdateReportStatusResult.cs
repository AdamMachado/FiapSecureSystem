using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.UpdateReportStatus;

public sealed record UpdateReportStatusResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    ReportStatus PreviousStatus,
    ReportStatus CurrentStatus,
    DateTime UpdatedAtUtc,
    string? FailureReason);