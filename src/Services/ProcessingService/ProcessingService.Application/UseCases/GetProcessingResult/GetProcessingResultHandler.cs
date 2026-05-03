using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Mappings;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ProcessingService.Application.UseCases.GetProcessingResult;

public sealed class GetProcessingResultHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly ILogger<GetProcessingResultHandler> _logger;
    private readonly ActivitySource _activitySource;

    public GetProcessingResultHandler(
        IAnalysisProcessRepository repository,
        ILogger<GetProcessingResultHandler> logger,
        ActivitySource activitySource)
    {
        _repository = repository;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task<Result<GetProcessingResultResult>> HandleAsync(
        GetProcessingResultQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ProcessingService get processing result",
            ActivityKind.Internal);

        activity?.SetTag("analysis.request.id", query.AnalysisRequestId);

        _logger.LogInformation(
            "Handling {QueryType} for AnalysisRequestId={AnalysisRequestId}",
            nameof(GetProcessingResultQuery),
            query.AnalysisRequestId);

        try
        {
            var process = await _repository.GetByAnalysisRequestIdAsync(
                AnalysisRequestId.Create(query.AnalysisRequestId),
                cancellationToken);

            if (process is null)
            {
                const string message = "Processing result was not found.";

                activity?.SetTag("processing.result.found", false);
                activity?.SetStatus(ActivityStatusCode.Error, message);
                activity?.SetTag("exception.type", typeof(NotFoundException).FullName);
                activity?.SetTag("exception.message", $"Processing result for analysis request '{query.AnalysisRequestId}' was not found.");

                _logger.LogWarning(
                    "No analysis process found for AnalysisRequestId={AnalysisRequestId}. Cannot retrieve processing result.",
                    query.AnalysisRequestId);

                return Result.Failure<GetProcessingResultResult>(
                    Error.NotFound(
                        "processing.not_found",
                        $"Processing result for analysis request '{query.AnalysisRequestId}' was not found."));
            }

            activity?.SetTag("processing.result.found", true);
            activity?.SetTag("processing.process.id", process.Id);
            activity?.SetTag("processing.status", process.Status.ToString());
            activity?.SetTag("analysis.components.count", process.Components.Count);
            activity?.SetTag("analysis.risks.count", process.Risks.Count);
            activity?.SetTag("analysis.recommendations.count", process.Recommendations.Count);
            activity?.SetTag("processing.has_failure_reason", !string.IsNullOrWhiteSpace(process.FailureReason));
            activity?.SetTag("processing.has_failure_details", !string.IsNullOrWhiteSpace(process.FailureDetails));

            _logger.LogInformation(
                "Retrieved analysis process for AnalysisRequestId={AnalysisRequestId}. Current status: {Status}",
                query.AnalysisRequestId,
                process.Status);

            var result = process.ToResult();

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            _logger.LogError(
                ex,
                "Failed to retrieve processing result for AnalysisRequestId={AnalysisRequestId}.",
                query.AnalysisRequestId);

            var errorCode = "processing.error";

            if (ex is ValidationException)
                errorCode = "processing.validation_error";
            else if (ex is DomainException)
                errorCode = "processing.domain_error";
            else if (ex is ArgumentException)
                errorCode = "processing.invalid_argument";

            return Result.Failure<GetProcessingResultResult>(
                Error.Validation(errorCode, ex.Message));
        }
    }
}
