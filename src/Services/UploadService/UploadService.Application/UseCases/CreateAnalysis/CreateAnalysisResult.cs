using UploadService.Domain.Enums;

namespace UploadService.Application.UseCases.CreateAnalysis;

public sealed record CreateAnalysisResult(
    Guid AnalysisRequestId,
    AnalysisStatus Status,
    DateTime CreatedAtUtc);