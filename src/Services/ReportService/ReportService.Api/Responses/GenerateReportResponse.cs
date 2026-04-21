using ReportService.Domain.Enums;
using Shared.Kernel.Result;

namespace ReportService.Api.Contracts.Responses;

public sealed record GenerateReportResponse(
    Guid ReportId,
    Guid AnalysisRequestId,
    ReportFormat Format,
    ReportStatus Status,
    string FileName,
    DateTime? GeneratedAtUtc);