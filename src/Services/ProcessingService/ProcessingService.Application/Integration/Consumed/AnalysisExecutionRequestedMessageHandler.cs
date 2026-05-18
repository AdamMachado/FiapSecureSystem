using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Exceptions;
using ProcessingService.Application.UseCases.ExecuteAnalysisProcessing;
using Shared.Contracts.IntegrationEvents;

namespace ProcessingService.Application.Integration.Consumed;

public sealed class AnalysisExecutionRequestedMessageHandler
    : IIntegrationEventHandler<AnalysisExecutionRequestedIntegrationEvent>
{
    private readonly ExecuteAnalysisProcessingHandler _handler;
    private readonly ILogger<AnalysisExecutionRequestedMessageHandler> _logger;

    public AnalysisExecutionRequestedMessageHandler(
        ExecuteAnalysisProcessingHandler handler,
        ILogger<AnalysisExecutionRequestedMessageHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task HandleAsync(
        AnalysisExecutionRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Received {EventType} for deferred analysis execution. AnalysisProcessId={AnalysisProcessId}, AnalysisRequestId={AnalysisRequestId}",
            nameof(AnalysisExecutionRequestedIntegrationEvent),
            integrationEvent.AnalysisProcessId,
            integrationEvent.AnalysisRequestId);

        var command = new ExecuteAnalysisProcessingCommand(
            integrationEvent.AnalysisProcessId,
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
                "Failed to execute analysis processing for AnalysisRequestId={AnalysisRequestId}. Error: {ErrorCode} - {ErrorMessage}",
                integrationEvent.AnalysisRequestId,
                result.Error.Code,
                result.Error.Message);

            throw new MessageHandlingException(
                $"Failed to process {nameof(AnalysisExecutionRequestedIntegrationEvent)} for analysis '{integrationEvent.AnalysisRequestId}'. " +
                $"Error: {result.Error.Code} - {result.Error.Message}",
                result.Error.Code);
        }

        _logger.LogInformation(
            "Successfully executed deferred analysis processing for AnalysisRequestId={AnalysisRequestId}",
            integrationEvent.AnalysisRequestId);
    }
}
