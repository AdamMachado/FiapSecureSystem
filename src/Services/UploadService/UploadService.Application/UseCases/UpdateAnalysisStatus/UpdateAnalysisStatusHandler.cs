using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Domain.Enums;

namespace UploadService.Application.UseCases.UpdateAnalysisStatus;

public sealed class UpdateAnalysisStatusHandler
{
    private readonly IAnalysisRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateAnalysisStatusHandler(
        IAnalysisRequestRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UpdateAnalysisStatusResult>> HandleAsync(
        UpdateAnalysisStatusCommand command,
        CancellationToken cancellationToken = default)
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

        try
        {
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

            return Result.Success(new UpdateAnalysisStatusResult(
                analysisRequest.Id,
                previousStatus,
                analysisRequest.Status,
                analysisRequest.UpdatedAtUtc,
                analysisRequest.FailureReason));
        }
        catch (DomainException ex)
        {
            return Result.Failure<UpdateAnalysisStatusResult>(
                Error.Conflict(
                    "analysis_status.invalid_transition",
                    ex.Message));
        }
        catch (ValidationException ex)
        {
            return Result.Failure<UpdateAnalysisStatusResult>(
                Error.Validation(
                    "analysis_status.validation_error",
                    ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<UpdateAnalysisStatusResult>(
                Error.Validation(
                    "analysis_status.invalid_argument",
                    ex.Message));
        }
    }
}