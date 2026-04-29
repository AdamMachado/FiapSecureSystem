using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using ReportService.Application.Exceptions;
using Shared.Contracts.IntegrationEvents;

namespace ProcessingService.Application.Integration.Consumed;

public sealed class AnalysisRequestedMessageHandler
    : IIntegrationEventHandler<AnalysisRequestedIntegrationEvent>
{
    private readonly StartAnalysisProcessingHandler _handler;
    private readonly ILogger<AnalysisRequestedMessageHandler> _logger;

    public AnalysisRequestedMessageHandler(StartAnalysisProcessingHandler handler, ILogger<AnalysisRequestedMessageHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task HandleAsync(
        AnalysisRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Received {EventType} for analysis request. AnalysisRequestId={AnalysisRequestId}, RequestedByUserId={RequestedByUserId}",
            nameof(AnalysisRequestedIntegrationEvent),
            integrationEvent.AnalysisRequestId,
            integrationEvent.RequestedByUserId);

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
            _logger.LogError(
                "Failed to start analysis processing for AnalysisRequestId={AnalysisRequestId}. Error: {ErrorCode} - {ErrorMessage}",
                integrationEvent.AnalysisRequestId,
                result.Error.Code,
                result.Error.Message);

            throw new MessageHandlingException(
                $"Failed to process {nameof(AnalysisFailedIntegrationEvent)} for analysis '{integrationEvent.AnalysisRequestId}'. " +
                $"Error: {result.Error.Code} - {result.Error.Message}",
                result.Error.Code);
        }

        _logger.LogInformation(
            "Successfully started analysis processing for AnalysisRequestId={AnalysisRequestId}",
            integrationEvent.AnalysisRequestId);
    }
}
