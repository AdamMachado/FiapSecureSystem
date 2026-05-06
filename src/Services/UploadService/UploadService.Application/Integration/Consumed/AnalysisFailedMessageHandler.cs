using Shared.Contracts.IntegrationEvents;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Exceptions;
using UploadService.Application.UseCases.UpdateAnalysisStatus;
using UploadService.Domain.Enums;

namespace UploadService.Application.Integration.Consumed;

public sealed class AnalysisFailedMessageHandler
    : IIntegrationEventHandler<AnalysisFailedIntegrationEvent>
{
    private readonly UpdateAnalysisStatusHandler _updateAnalysisStatusHandler;

    public AnalysisFailedMessageHandler(UpdateAnalysisStatusHandler updateAnalysisStatusHandler)
    {
        _updateAnalysisStatusHandler = updateAnalysisStatusHandler;
    }

    public async Task HandleAsync(
        AnalysisFailedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateAnalysisStatusCommand(
            integrationEvent.AnalysisRequestId,
            AnalysisStatus.Failed,
            integrationEvent.Reason);

        var result = await _updateAnalysisStatusHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new MessageHandlingException(
                $"Failed to process {nameof(AnalysisFailedIntegrationEvent)} for analysis '{integrationEvent.AnalysisRequestId}'. " +
                $"Error: {result.Error.Code} - {result.Error.Message}",
                result.Error.Code);
        }
    }
}