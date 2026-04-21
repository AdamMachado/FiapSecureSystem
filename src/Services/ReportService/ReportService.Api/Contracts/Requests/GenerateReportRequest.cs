using ReportService.Domain.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ReportService.Api.Contracts.Requests;

public sealed record GenerateReportRequest(
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    AnalysisResultDto Result,
    ReportFormat Format);