using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using Shared.Contracts.IntegrationEvents;

namespace ProcessingService.Application.Integration.Consumed;

public sealed class AnalysisRequestedMessageHandler
    : IIntegrationEventHandler<AnalysisRequestedIntegrationEvent>
{
    private readonly StartAnalysisProcessingHandler _handler;

    public AnalysisRequestedMessageHandler(StartAnalysisProcessingHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleAsync(
        AnalysisRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        await _handler.HandleAsync(
            new StartAnalysisProcessingCommand(
                integrationEvent.AnalysisRequestId,
                integrationEvent.RequestedByUserId,
                integrationEvent.FileName,
                integrationEvent.ContentType,
                integrationEvent.FileHash,
                integrationEvent.StorageBucket,
                integrationEvent.StorageObjectKey),
            cancellationToken);
    }
}
