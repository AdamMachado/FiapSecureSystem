using ProcessingService.Application.UseCases.GetProcessingResult;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ProcessingService.Application.Mappings;

public static class AnalysisProcessMappings
{
    public static GetProcessingResultResult ToResult(this AnalysisProcess process)
        => new(
            process.Id,
            process.AnalysisRequestId.Value,
            process.RequestedByUserId,
            process.Status,
            process.DiagramType,
            process.SourceFileLocation.BucketName,
            process.SourceFileLocation.ObjectKey,
            process.ExtractedText?.Value,
            process.Components,
            process.Risks,
            process.Recommendations,
            process.ResultSummary is null ? null : ToSummaryDto(process.ResultSummary),
            process.CreatedAtUtc,
            process.UpdatedAtUtc,
            process.StartedAtUtc,
            process.CompletedAtUtc,
            process.FailedAtUtc,
            process.FailureReason,
            process.FailureDetails);

    public static AnalysisSummaryDto ToSummaryDto(ProcessingResultSummary summary)
        => new(
            summary.Overview,
            summary.TotalComponents,
            summary.TotalRisks,
            summary.TotalRecommendations,
            summary.RequiresManualReview,
            summary.Warnings);
}
