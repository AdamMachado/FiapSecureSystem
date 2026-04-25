namespace ProcessingService.Application.UseCases.FailAnalysisProcessing;

public sealed record FailAnalysisProcessingCommand(
    Guid AnalysisRequestId,
    string Reason,
    string? Details = null);
