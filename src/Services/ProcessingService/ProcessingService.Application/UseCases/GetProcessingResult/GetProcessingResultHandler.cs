using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Mappings;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.GetProcessingResult;

public sealed class GetProcessingResultHandler
{
    private readonly IAnalysisProcessRepository _repository;

    public GetProcessingResultHandler(IAnalysisProcessRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetProcessingResultResult>> HandleAsync(
        GetProcessingResultQuery query,
        CancellationToken cancellationToken = default)
    {
        var process = await _repository.GetByAnalysisRequestIdAsync(query.AnalysisRequestId, cancellationToken);
        if (process is null)
        {
            return Result.Failure<GetProcessingResultResult>(
                Error.NotFound(
                    "processing.not_found",
                    $"Processing result for analysis request '{query.AnalysisRequestId}' was not found."));
        }

        return Result.Success(process.ToResult());
    }
}
