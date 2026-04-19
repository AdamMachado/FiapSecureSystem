using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Domain.Enums;
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
            integrationEvent.Result,
            ReportFormat.Markdown);

        await _generateReportHandler.HandleAsync(command, cancellationToken);
    }
}