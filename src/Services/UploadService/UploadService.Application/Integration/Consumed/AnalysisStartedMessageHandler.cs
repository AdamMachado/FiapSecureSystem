using Shared.Contracts.IntegrationEvents;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Exceptions;
using UploadService.Application.UseCases.UpdateAnalysisStatus;
using UploadService.Domain.Enums;

namespace UploadService.Application.Integration.Consumed;

public sealed class AnalysisStartedMessageHandler
    : IIntegrationEventHandler<AnalysisStartedIntegrationEvent>
{
    private readonly UpdateAnalysisStatusHandler _updateAnalysisStatusHandler;

    public AnalysisStartedMessageHandler(UpdateAnalysisStatusHandler updateAnalysisStatusHandler)
    {
        _updateAnalysisStatusHandler = updateAnalysisStatusHandler;
    }

    public async Task HandleAsync(
        AnalysisStartedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateAnalysisStatusCommand(
            integrationEvent.AnalysisRequestId,
            AnalysisStatus.Processing);

        var result = await _updateAnalysisStatusHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new MessageHandlingException(
                $"Failed to process {nameof(AnalysisStartedIntegrationEvent)} for analysis '{integrationEvent.AnalysisRequestId}'. " +
                $"Error: {result.Error.Code} - {result.Error.Message}",
                result.Error.Code);
        }
    }
}