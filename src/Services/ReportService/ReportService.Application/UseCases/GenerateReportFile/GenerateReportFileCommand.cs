using ReportService.Domain.Enums;

namespace ReportService.Application.UseCases.GenerateReportFile;

public sealed record GenerateReportFileCommand(
    Guid AnalysisRequestId,
    ReportFormat Format);
