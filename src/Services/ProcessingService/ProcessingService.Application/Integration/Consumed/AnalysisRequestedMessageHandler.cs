using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using ReportService.Application.Exceptions;
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
        var command = new StartAnalysisProcessingCommand(
            integrationEvent.AnalysisRequestId,
            integrationEvent.RequestedByUserId,
            integrationEvent.FileName,
            integrationEvent.ContentType,
            integrationEvent.FileHash,
            integrationEvent.StorageBucket,
            integrationEvent.StorageObjectKey);

        var result = await _handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new MessageHandlingException(
                $"Failed to process {nameof(AnalysisFailedIntegrationEvent)} for analysis '{integrationEvent.AnalysisRequestId}'. " +
                $"Error: {result.Error.Code} - {result.Error.Message}",
                result.Error.Code);
        }
    }
}
