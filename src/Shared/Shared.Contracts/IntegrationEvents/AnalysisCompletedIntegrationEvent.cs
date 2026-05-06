using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace Shared.Contracts.IntegrationEvents;

public sealed class AnalysisCompletedIntegrationEvent : IntegrationEventBase
{
    public AnalysisCompletedIntegrationEvent(
        Guid correlationId,
        Guid? causationId,
        Guid analysisRequestId,
        Guid requestedByUserId,
        DateTime completedAtUtc,
        AnalysisResultDto result)
        : base(correlationId, causationId)
    {
        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        CompletedAtUtc = completedAtUtc;
        Result = result;
    }

    public Guid AnalysisRequestId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public AnalysisResultDto Result { get; init; }
}