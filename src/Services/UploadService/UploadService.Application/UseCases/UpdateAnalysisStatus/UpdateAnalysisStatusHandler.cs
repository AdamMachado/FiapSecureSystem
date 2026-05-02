using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;
using System.Diagnostics;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Domain.Enums;

namespace UploadService.Application.UseCases.UpdateAnalysisStatus;

public sealed class UpdateAnalysisStatusHandler
{
    private readonly IAnalysisRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ActivitySource _activitySource;

    public UpdateAnalysisStatusHandler(
        IAnalysisRequestRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ActivitySource activitySource)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _activitySource = activitySource;
    }

    public async Task<Result<UpdateAnalysisStatusResult>> HandleAsync(
        UpdateAnalysisStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
        "UploadService update analysis status",
        ActivityKind.Internal);

        activity?.SetTag("analysis.request.id", command.AnalysisRequestId);
        activity?.SetTag("analysis.target_status", command.TargetStatus.ToString());

        try
        {
            var analysisRequest = await _repository.GetByIdAsync(command.AnalysisRequestId, cancellationToken);

            if (analysisRequest is null)
            {
                return Result.Failure<UpdateAnalysisStatusResult>(
                    Error.NotFound(
                        "analysis_request.not_found",
                        $"Analysis request '{command.AnalysisRequestId}' was not found."));
            }

            if (command.TargetStatus is AnalysisStatus.Received)
            {
                return Result.Failure<UpdateAnalysisStatusResult>(
                    Error.Validation(
                        "analysis_status.invalid_target",
                        "Target status is not valid for update."));
            }

            var previousStatus = analysisRequest.Status;
            var now = _dateTimeProvider.UtcNow;

            switch (command.TargetStatus)
            {
                case AnalysisStatus.Processing:
                    analysisRequest.MarkAsProcessing(now);
                    break;

                case AnalysisStatus.Completed:
                    analysisRequest.MarkAsCompleted(now);
                    break;

                case AnalysisStatus.Failed:
                    analysisRequest.MarkAsFailed(
                        command.FailureReason ?? "Unknown processing failure.",
                        now);
                    break;

                default:
                    return Result.Failure<UpdateAnalysisStatusResult>(
                        Error.Validation(
                            "analysis_status.invalid_target",
                            "Target status is not valid for update."));
            }

            _repository.Update(analysisRequest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new UpdateAnalysisStatusResult(
                analysisRequest.Id,
                previousStatus,
                analysisRequest.Status,
                analysisRequest.UpdatedAtUtc,
                analysisRequest.FailureReason));
        }
        catch(Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            var errorCode = "analysis_status.error";

            if (ex is DomainException)
            {
                errorCode = "analysis_status.invalid_transition";

                return Result.Failure<UpdateAnalysisStatusResult>(
                Error.Conflict(
                    errorCode,
                    ex.Message));
            }
            else if (ex is ValidationException)
                errorCode = "analysis_status.validation_error";
            else if (ex is ArgumentException)
                errorCode = "analysis_status.invalid_argument";

            return Result.Failure<UpdateAnalysisStatusResult>(
                Error.Validation(
                    errorCode,
                    ex.Message));
        }
    }
}