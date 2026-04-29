using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Mappings;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.GetProcessingResult;

public sealed class GetProcessingResultHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly ILogger<GetProcessingResultHandler> _logger;

    public GetProcessingResultHandler(IAnalysisProcessRepository repository, ILogger<GetProcessingResultHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<GetProcessingResultResult>> HandleAsync(
        GetProcessingResultQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling {QueryType} for AnalysisRequestId={AnalysisRequestId}",
            nameof(GetProcessingResultQuery),
            query.AnalysisRequestId);

        var process = await _repository.GetByAnalysisRequestIdAsync(
            AnalysisRequestId.Create(query.AnalysisRequestId), 
            cancellationToken);

        if (process is null)
        {
            _logger.LogWarning(
                "No analysis process found for AnalysisRequestId={AnalysisRequestId}. Cannot retrieve processing result.",
                query.AnalysisRequestId);

            return Result.Failure<GetProcessingResultResult>(
                Error.NotFound(
                    "processing.not_found",
                    $"Processing result for analysis request '{query.AnalysisRequestId}' was not found."));
        }

        _logger.LogInformation(
            "Retrieved analysis process for AnalysisRequestId={AnalysisRequestId}. Current status: {Status}",
            query.AnalysisRequestId,
            process.Status);

        return Result.Success(process.ToResult());
    }
}
