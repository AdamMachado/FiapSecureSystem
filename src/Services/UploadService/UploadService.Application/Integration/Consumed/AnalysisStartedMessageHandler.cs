using Shared.Contracts.IntegrationEvents;
using UploadService.Application.Abstractions.Messaging;
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

        await _updateAnalysisStatusHandler.HandleAsync(command, cancellationToken);
    }
}