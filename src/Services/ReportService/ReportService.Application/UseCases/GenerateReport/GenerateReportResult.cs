namespace ReportService.Application.UseCases.GenerateReport;

public sealed record GenerateReportResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
