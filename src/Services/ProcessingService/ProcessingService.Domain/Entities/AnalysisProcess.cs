using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Schemas;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;
using System.Text.Json;

namespace ProcessingService.Domain.Entities;

public sealed class AnalysisProcess : AggregateRoot<Guid>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string _componentsJson = "[]";
    private string _risksJson = "[]";
    private string _recommendationsJson = "[]";

    private AnalysisProcess()
    {
    }

    private AnalysisProcess(
        Guid id,
        AnalysisRequestId analysisRequestId,
        Guid requestedByUserId,
        SourceFileLocation sourceFileLocation,
        DiagramType diagramType,
        DateTime createdAtUtc)
        : base(id)
    {
        if (requestedByUserId == Guid.Empty)
            throw new ValidationException("Requested by user id must be a non-empty GUID.");

        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        SourceFileLocation = sourceFileLocation;
        DiagramType = diagramType;
        Status = ProcessingStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public AnalysisRequestId AnalysisRequestId { get; private set; } = default!;
    public Guid RequestedByUserId { get; private set; }
    public SourceFileLocation SourceFileLocation { get; private set; } = default!;
    public DiagramType DiagramType { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public ExtractedText? ExtractedText { get; private set; }
    public ProcessingResultSummary? ResultSummary { get; private set; }
    public string? FailureReason { get; private set; }
    public string? FailureDetails { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }

    public IReadOnlyCollection<IdentifiedComponentDto> Components =>
        JsonSerializer.Deserialize<IReadOnlyCollection<IdentifiedComponentDto>>(_componentsJson, JsonOptions)
        ?? Array.Empty<IdentifiedComponentDto>();

    public IReadOnlyCollection<ArchitecturalRiskDto> Risks =>
        JsonSerializer.Deserialize<IReadOnlyCollection<ArchitecturalRiskDto>>(_risksJson, JsonOptions)
        ?? Array.Empty<ArchitecturalRiskDto>();

    public IReadOnlyCollection<ArchitecturalRecommendationDto> Recommendations =>
        JsonSerializer.Deserialize<IReadOnlyCollection<ArchitecturalRecommendationDto>>(_recommendationsJson, JsonOptions)
        ?? Array.Empty<ArchitecturalRecommendationDto>();

    public static AnalysisProcess Create(
        Guid id,
        AnalysisRequestId analysisRequestId,
        Guid requestedByUserId,
        SourceFileLocation sourceFileLocation,
        DiagramType diagramType,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Analysis process id must be a non-empty GUID.");

        return new AnalysisProcess(
            id,
            analysisRequestId,
            requestedByUserId,
            sourceFileLocation,
            diagramType,
            createdAtUtc);
    }

    public void MarkAsStarted(DateTime startedAtUtc)
    {
        if (Status is ProcessingStatus.Processing or ProcessingStatus.Completed)
            throw new DiagramProcessingException($"Cannot start analysis processing when current status is '{Status}'.");

        if (Status == ProcessingStatus.Failed)
            throw new DiagramProcessingException("Cannot start a failed analysis process. Create a new processing attempt instead.");

        Status = ProcessingStatus.Processing;
        FailureReason = null;
        FailureDetails = null;
        StartedAtUtc ??= startedAtUtc;
        UpdatedAtUtc = startedAtUtc;

        RaiseDomainEvent(new AnalysisProcessingStartedDomainEvent(
            Id,
            AnalysisRequestId,
            RequestedByUserId,
            SourceFileLocation,
            DiagramType,
            startedAtUtc));
    }

    public void MarkAsCompleted(
        ExtractedText extractedText,
        IReadOnlyCollection<IdentifiedComponentDto> components,
        IReadOnlyCollection<ArchitecturalRiskDto> risks,
        IReadOnlyCollection<ArchitecturalRecommendationDto> recommendations,
        ProcessingResultSummary resultSummary,
        DateTime completedAtUtc)
    {
        EnsureCanTransitionToCompleted();
        EnsureValidAnalysisResult(components, risks, recommendations, resultSummary);

        ExtractedText = extractedText;
        ResultSummary = resultSummary;
        FailureReason = null;
        FailureDetails = null;

        _componentsJson = JsonSerializer.Serialize(components ?? Array.Empty<IdentifiedComponentDto>(), JsonOptions);
        _risksJson = JsonSerializer.Serialize(risks ?? Array.Empty<ArchitecturalRiskDto>(), JsonOptions);
        _recommendationsJson = JsonSerializer.Serialize(recommendations ?? Array.Empty<ArchitecturalRecommendationDto>(), JsonOptions);

        Status = ProcessingStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;

        RaiseDomainEvent(new AnalysisProcessingCompletedDomainEvent(
            Id,
            AnalysisRequestId,
            RequestedByUserId,
            DiagramType,
            extractedText,
            Components,
            Risks,
            Recommendations,
            resultSummary,
            completedAtUtc));
    }

    public void MarkAsFailed(string reason, string? details, DateTime failedAtUtc)
    {
        if (Status == ProcessingStatus.Completed)
            throw new DiagramProcessingException("Cannot fail an analysis process that has already been completed.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("Failure reason is required.");

        Status = ProcessingStatus.Failed;
        FailureReason = reason.Trim();
        FailureDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        FailedAtUtc = failedAtUtc;
        UpdatedAtUtc = failedAtUtc;

        RaiseDomainEvent(new AnalysisProcessingFailedDomainEvent(
            Id,
            AnalysisRequestId,
            RequestedByUserId,
            FailureReason,
            FailureDetails,
            failedAtUtc));
    }

    public void ResetForRetry(DateTime updatedAtUtc)
    {
        if (Status != ProcessingStatus.Failed)
            throw new DiagramProcessingException("Only failed analysis processes can be reset for retry.");

        Status = ProcessingStatus.Pending;
        FailureReason = null;
        FailureDetails = null;
        StartedAtUtc = null;
        CompletedAtUtc = null;
        FailedAtUtc = null;
        ExtractedText = null;
        ResultSummary = null;
        _componentsJson = "[]";
        _risksJson = "[]";
        _recommendationsJson = "[]";
        UpdatedAtUtc = updatedAtUtc;
    }

    private void EnsureCanTransitionToCompleted()
    {
        if (Status == ProcessingStatus.Pending)
            throw new DiagramProcessingException("Cannot complete analysis processing before it has started.");

        if (Status == ProcessingStatus.Completed)
            throw new DiagramProcessingException("Analysis processing has already been completed.");

        if (Status == ProcessingStatus.Failed)
            throw new DiagramProcessingException("Cannot complete a failed analysis process without resetting it first.");
    }

    private static void EnsureValidAnalysisResult(
        IReadOnlyCollection<IdentifiedComponentDto> components,
        IReadOnlyCollection<ArchitecturalRiskDto> risks,
        IReadOnlyCollection<ArchitecturalRecommendationDto> recommendations,
        ProcessingResultSummary summary)
    {
        if (components is null || components.Count == 0)
            throw new InvalidAnalysisResultException("At least one identified component must be present.");

        if (summary.TotalComponents != components.Count)
            throw new InvalidAnalysisResultException(
                $"Summary total components ({summary.TotalComponents}) does not match the provided collection size ({components.Count}).");

        if (summary.TotalRisks != risks.Count)
            throw new InvalidAnalysisResultException(
                $"Summary total risks ({summary.TotalRisks}) does not match the provided collection size ({risks.Count}).");

        if (summary.TotalRecommendations != recommendations.Count)
            throw new InvalidAnalysisResultException(
                $"Summary total recommendations ({summary.TotalRecommendations}) does not match the provided collection size ({recommendations.Count}).");

        var duplicateComponentIds = components
            .GroupBy(static x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        if (duplicateComponentIds.Length > 0)
            throw new InvalidAnalysisResultException($"Duplicate component ids were found: {string.Join(", ", duplicateComponentIds)}.");

        var duplicateRiskIds = risks
            .GroupBy(static x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        if (duplicateRiskIds.Length > 0)
            throw new InvalidAnalysisResultException($"Duplicate risk ids were found: {string.Join(", ", duplicateRiskIds)}.");

        var duplicateRecommendationIds = recommendations
            .GroupBy(static x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        if (duplicateRecommendationIds.Length > 0)
            throw new InvalidAnalysisResultException($"Duplicate recommendation ids were found: {string.Join(", ", duplicateRecommendationIds)}.");
    }
}
