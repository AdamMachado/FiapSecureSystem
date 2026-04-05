using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.Models;

namespace Shared.Contracts.IntegrationEvents;

public sealed record AnalysisCompletedIntegrationEvent : IntegrationEventBase
{
    public AnalysisCompletedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid jobId,
        DateTime completedAtUtc,
        IReadOnlyCollection<AnalysisRiskItem> risks,
        string summary)
        : base(correlationId, causationId)
    {
        JobId = jobId;
        CompletedAtUtc = completedAtUtc;
        Risks = risks;
        Summary = summary;
    }

    public override string EventType => nameof(AnalysisCompletedIntegrationEvent);

    public Guid JobId { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public IReadOnlyCollection<AnalysisRiskItem> Risks { get; init; }
    public string Summary { get; init; }
}