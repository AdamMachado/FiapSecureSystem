using Shared.Kernel.Exceptions;
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

    public async Task<UpdateAnalysisStatusResult> HandleAsync(
        UpdateAnalysisStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var analysisRequest = await _repository.GetByIdAsync(command.AnalysisRequestId, cancellationToken);

        if (analysisRequest is null)
            throw new NotFoundException($"Analysis request '{command.AnalysisRequestId}' was not found.");

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
                analysisRequest.MarkAsFailed(command.FailureReason ?? "Unknown processing failure.", now);
                break;

            case AnalysisStatus.Received:
            default:
                throw new ValidationException("Target status is not valid for update.");
        }

        _repository.Update(analysisRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateAnalysisStatusResult(
            analysisRequest.Id,
            previousStatus,
            analysisRequest.Status,
            analysisRequest.UpdatedAtUtc,
            analysisRequest.FailureReason);
    }
}