using System.Text.Json;

namespace ReportService.Api.Contracts.Responses;

public sealed record GetReportByAnalysisResponse(
    Guid ReportId,
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    JsonElement AnalysisData,
    IReadOnlyCollection<AnalysisReportFileResponse> Files,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
