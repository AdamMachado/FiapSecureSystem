using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.UpdateReportStatus;

public sealed record UpdateReportStatusCommand(
    Guid AnalysisRequestId,
    ReportStatus TargetStatus,
    string? FailureReason = null);