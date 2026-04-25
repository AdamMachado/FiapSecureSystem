using ProcessingService.Domain.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ProcessingService.Application.UseCases.GetProcessingResult;

public sealed record GetProcessingResultResult(
    Guid AnalysisProcessId,
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    ProcessingStatus Status,
    DiagramType DiagramType,
    string SourceBucketName,
    string SourceObjectKey,
    string? ExtractedText,
    IReadOnlyCollection<IdentifiedComponentDto> Components,
    IReadOnlyCollection<ArchitecturalRiskDto> Risks,
    IReadOnlyCollection<ArchitecturalRecommendationDto> Recommendations,
    AnalysisSummaryDto? Summary,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? FailedAtUtc,
    string? FailureReason,
    string? FailureDetails);
