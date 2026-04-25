using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Domain.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Correlation;

namespace ProcessingService.Application.Integration.Published;

public sealed class AnalysisFailedIntegrationEventMapper
    : IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent>
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public AnalysisFailedIntegrationEventMapper(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public IntegrationEventBase Map(AnalysisProcessingFailedDomainEvent domainEvent)
        => new AnalysisFailedIntegrationEvent(
            _correlationContextAccessor.GetOrCreateCorrelationGuid(),
            _correlationContextAccessor.GetCausationGuidOrNull(),
            domainEvent.AnalysisRequestId.Value,
            domainEvent.RequestedByUserId,
            domainEvent.FailedAtUtc,
            domainEvent.Reason,
            domainEvent.Details);
}
