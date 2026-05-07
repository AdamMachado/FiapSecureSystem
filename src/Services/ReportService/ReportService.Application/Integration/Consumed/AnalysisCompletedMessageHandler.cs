using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Exceptions;
using ReportService.Application.UseCases.GenerateReport;
using Shared.Contracts.IntegrationEvents;

namespace ReportService.Application.Integration.Consumed;

public sealed class AnalysisCompletedMessageHandler
    : IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>
{
    private readonly GenerateReportHandler _generateReportHandler;

    public AnalysisCompletedMessageHandler(GenerateReportHandler generateReportHandler)
    {
        _generateReportHandler = generateReportHandler;
    }

    public async Task HandleAsync(
        AnalysisCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var command = new GenerateReportCommand(
            integrationEvent.AnalysisRequestId,
            integrationEvent.RequestedByUserId,
            integrationEvent.Result);

        var result = await _generateReportHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new MessageHandlingException(
                $"Failed to process {nameof(AnalysisCompletedIntegrationEvent)} for analysis '{integrationEvent.AnalysisRequestId}'. " +
                $"Error: {result.Error.Code} - {result.Error.Message}",
                result.Error.Code);
        }
    }
}
