using Shared.Contracts.IntegrationEvents;
using Shared.Observability.Correlation;

namespace ProcessingService.Application.Integration.Published;

public sealed class AnalysisExecutionRequestedIntegrationEventFactory
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public AnalysisExecutionRequestedIntegrationEventFactory(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public AnalysisExecutionRequestedIntegrationEvent Create(
        UseCases.StartAnalysisProcessing.StartAnalysisProcessingCommand command,
        Guid analysisProcessId)
    {
        return new AnalysisExecutionRequestedIntegrationEvent(
            _correlationContextAccessor.GetOrCreateCorrelationGuid(),
            _correlationContextAccessor.GetCausationGuidOrNull(),
            analysisProcessId,
            command.AnalysisRequestId,
            command.RequestedByUserId,
            command.FileName,
            command.ContentType,
            command.FileHash,
            command.StorageBucket,
            command.StorageObjectKey);
    }
}
