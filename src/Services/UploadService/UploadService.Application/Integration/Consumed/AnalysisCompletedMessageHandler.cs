using Shared.Contracts.IntegrationEvents;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.UseCases.UpdateAnalysisStatus;
using UploadService.Domain.Enums;

namespace UploadService.Application.Integration.Consumed;

public sealed class AnalysisCompletedMessageHandler
    : IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>
{
    private readonly UpdateAnalysisStatusHandler _updateAnalysisStatusHandler;

    public AnalysisCompletedMessageHandler(UpdateAnalysisStatusHandler updateAnalysisStatusHandler)
    {
        _updateAnalysisStatusHandler = updateAnalysisStatusHandler;
    }

    public async Task HandleAsync(
        AnalysisCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateAnalysisStatusCommand(
            integrationEvent.AnalysisRequestId,
            AnalysisStatus.Completed);

        await _updateAnalysisStatusHandler.HandleAsync(command, cancellationToken);
    }
}