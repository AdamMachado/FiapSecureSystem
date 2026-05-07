using Shared.Contracts.IntegrationEvents.Schemas;

namespace ReportService.Application.UseCases.GenerateReport;

public sealed record GenerateReportCommand(
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    AnalysisResultDto Result);
