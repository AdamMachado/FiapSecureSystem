using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Mappings;
using ProcessingService.Domain.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Schemas;
using Shared.Observability.Correlation;

namespace ProcessingService.Application.Integration.Published;

public sealed class AnalysisCompletedIntegrationEventMapper
    : IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent>
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public AnalysisCompletedIntegrationEventMapper(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public IntegrationEventBase Map(AnalysisProcessingCompletedDomainEvent domainEvent)
    {
        var result = new AnalysisResultDto(
            domainEvent.Components,
            domainEvent.Risks,
            domainEvent.Recommendations,
            AnalysisProcessMappings.ToSummaryDto(domainEvent.Summary));

        return new AnalysisCompletedIntegrationEvent(
            _correlationContextAccessor.GetOrCreateCorrelationGuid(),
            _correlationContextAccessor.GetCausationGuidOrNull(),
            domainEvent.AnalysisRequestId.Value,
            domainEvent.RequestedByUserId,
            domainEvent.CompletedAtUtc,
            result,
            domainEvent.Summary.Overview);
    }
}
