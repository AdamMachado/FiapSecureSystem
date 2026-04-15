using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Observability.Correlation;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Domain.Events;

namespace UploadService.Application.Integration.Published;

public sealed class AnalysisRequestedIntegrationEventMapper
    : IIntegrationEventMapper<AnalysisRequestCreatedDomainEvent>
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public AnalysisRequestedIntegrationEventMapper(
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public IntegrationEventBase Map(AnalysisRequestCreatedDomainEvent domainEvent)
    {
        return new AnalysisRequestedIntegrationEvent(
            correlationId: _correlationContextAccessor.GetOrCreateCorrelationGuid(),
            causationId: _correlationContextAccessor.GetCausationGuidOrNull(),
            analysisRequestId: domainEvent.AnalysisRequestId,
            requestedByUserId: domainEvent.RequestedByUserId,
            fileName: domainEvent.FileMetadata.FileName,
            contentType: domainEvent.FileMetadata.ContentType,
            fileHash: domainEvent.FileHash.Value,
            storageBucket: domainEvent.StorageLocation.BucketName,
            storageObjectKey: domainEvent.StorageLocation.ObjectKey);
    }
}