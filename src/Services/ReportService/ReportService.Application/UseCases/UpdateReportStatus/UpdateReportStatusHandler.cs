using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Domain.Enums;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;

namespace ReportService.Application.UseCases.UpdateReportStatus;

public sealed class UpdateReportStatusHandler
{
    private readonly IAnalysisReportRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateReportStatusHandler(
        IAnalysisReportRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UpdateReportStatusResult>> HandleAsync(
        UpdateReportStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var report = await _repository.GetByAnalysisRequestIdAsync(
            command.AnalysisRequestId,
            cancellationToken);

        if (report is null)
        {
            return Result.Failure<UpdateReportStatusResult>(
                Error.NotFound(
                    "report.not_found",
                    $"No report found for analysis request '{command.AnalysisRequestId}'."));
        }

        var previousStatus = report.Status;
        var now = _dateTimeProvider.UtcNow;

        try
        {
            switch (command.TargetStatus)
            {
                case ReportStatus.Generated:
                    report.MarkAsGenerated(now);
                    break;

                case ReportStatus.Failed:
                    report.MarkAsFailed(command.FailureReason ?? "Unknown report generation failure.", now);
                    break;

                default:
                    return Result.Failure<UpdateReportStatusResult>(
                        Error.Validation(
                            "report.invalid_target_status",
                            "Target status is not valid for update."));
            }

            _repository.Update(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new UpdateReportStatusResult(
                report.Id,
                report.AnalysisRequestId,
                previousStatus,
                report.Status,
                report.UpdatedAtUtc,
                report.FailureReason));
        }
        catch (DomainException ex)
        {
            return Result.Failure<UpdateReportStatusResult>(
                Error.Conflict(
                    "report.invalid_transition",
                    ex.Message));
        }
        catch (ValidationException ex)
        {
            return Result.Failure<UpdateReportStatusResult>(
                Error.Validation(
                    "report.validation_error",
                    ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<UpdateReportStatusResult>(
                Error.Validation(
                    "report.invalid_argument",
                    ex.Message));
        }
    }
}