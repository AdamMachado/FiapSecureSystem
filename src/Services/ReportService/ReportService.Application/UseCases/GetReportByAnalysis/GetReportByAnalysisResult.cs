using System.Text.Json;
using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.GetReportByAnalysis;

public sealed record GetReportByAnalysisResult(
    Guid ReportId,
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    JsonElement AnalysisData,
    IReadOnlyCollection<GetReportByAnalysisFileResult> Files,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record GetReportByAnalysisFileResult(
    ReportFormat Format,
    string FileName,
    string ContentType,
    string BucketName,
    string ObjectKey,
    DateTime GeneratedAtUtc);
