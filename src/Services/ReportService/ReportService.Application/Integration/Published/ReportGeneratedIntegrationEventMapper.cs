using ReportService.Application.Abstractions.Messaging;
using ReportService.Domain.Events;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Correlation;

namespace ReportService.Application.Integration.Published;

public sealed class ReportGeneratedIntegrationEventMapper
    : IIntegrationEventMapper<ReportGeneratedDomainEvent>
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public ReportGeneratedIntegrationEventMapper(
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public IntegrationEventBase Map(ReportGeneratedDomainEvent domainEvent)
    {
        return new ReportGeneratedIntegrationEvent(
            correlationId: _correlationContextAccessor.GetOrCreateCorrelationGuid(),
            causationId: _correlationContextAccessor.GetCausationGuidOrNull(),
            analysisRequestId: domainEvent.AnalysisRequestId,
            requestedByUserId: domainEvent.RequestedByUserId,
            reportId: domainEvent.ReportId,
            storageBucket: domainEvent.BucketName,
            storageObjectKey: domainEvent.ObjectKey,
            fileName: domainEvent.FileName,
            generatedAtUtc: domainEvent.GeneratedAtUtc);
    }
}