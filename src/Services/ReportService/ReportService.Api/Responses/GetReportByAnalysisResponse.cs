using ReportService.Domain.Enums;

namespace ReportService.Api.Contracts.Responses;

public sealed record GetReportByAnalysisResponse(
    Guid ReportId,
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    ReportFormat Format,
    ReportStatus Status,
    string FileName,
    string ContentType,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? GeneratedAtUtc,
    string? FailureReasony);