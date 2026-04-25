using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Domain.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Correlation;

namespace ProcessingService.Application.Integration.Published;

public sealed class AnalysisStartedIntegrationEventMapper
    : IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent>
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public AnalysisStartedIntegrationEventMapper(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public IntegrationEventBase Map(AnalysisProcessingStartedDomainEvent domainEvent)
        => new AnalysisStartedIntegrationEvent(
            _correlationContextAccessor.GetOrCreateCorrelationGuid(),
            _correlationContextAccessor.GetCausationGuidOrNull(),
            domainEvent.AnalysisRequestId.Value,
            domainEvent.RequestedByUserId,
            domainEvent.StartedAtUtc);
}
