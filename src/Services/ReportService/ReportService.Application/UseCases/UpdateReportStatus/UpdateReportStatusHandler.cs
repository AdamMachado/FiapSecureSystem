using Microsoft.Extensions.Logging;
using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Domain.Enums;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ReportService.Application.UseCases.UpdateReportStatus;

public sealed class UpdateReportStatusHandler
{
    private readonly IAnalysisReportRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ActivitySource _activitySource;
    private readonly ILogger<UpdateReportStatusHandler> _logger;

    public UpdateReportStatusHandler(
        IAnalysisReportRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ActivitySource activitySource,
        ILogger<UpdateReportStatusHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _activitySource = activitySource;
        _logger = logger;
    }

    public async Task<Result<UpdateReportStatusResult>> HandleAsync(
        UpdateReportStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ReportService update report status", 
            ActivityKind.Internal);

        activity?.SetTag("report.analysis_request_id", command.AnalysisRequestId);
        activity?.SetTag("report.target_status", command.TargetStatus.ToString());

        try
        {
            var report = await _repository.GetByAnalysisRequestIdAsync(
                command.AnalysisRequestId,
                cancellationToken);

            if (report is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Report not found");

                return Result.Failure<UpdateReportStatusResult>(
                    Error.NotFound(
                        "report.not_found",
                        $"No report found for analysis request '{command.AnalysisRequestId}'."));
            }

            var previousStatus = report.Status;
            var now = _dateTimeProvider.UtcNow;

            switch (command.TargetStatus)
            {
                case ReportStatus.Generated:
                    report.MarkAsGenerated(now);
                    break;

                case ReportStatus.Failed:
                    report.MarkAsFailed(command.FailureReason ?? "Unknown report generation failure.", now);
                    break;

                default:
                    activity?.SetStatus(ActivityStatusCode.Error, "Invalid target status");

                    return Result.Failure<UpdateReportStatusResult>(
                        Error.Validation(
                            "report.invalid_target_status",
                            "Target status is not valid for update."));
            }

            _repository.Update(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new UpdateReportStatusResult(
                report.Id,
                report.AnalysisRequestId,
                previousStatus,
                report.Status,
                report.UpdatedAtUtc,
                report.FailureReason));
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating report status for analysis request '{AnalysisRequestId}'.", command.AnalysisRequestId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            var errorCode = "report.error";

            if(ex is DomainException domainEx)
            {
                errorCode = $"report.invalid_transition";

                return Result.Failure<UpdateReportStatusResult>(
                    Error.Conflict(
                        errorCode,
                        ex.Message));
            }
            else if (ex is ValidationException validationEx)
                errorCode = $"report.validaion_error";
            else if (ex is ArgumentException argumentEx)
                errorCode = $"report.invalid_argument";

            return Result.Failure<UpdateReportStatusResult>(
                Error.Validation(
                    errorCode,
                    ex.Message));
        }
    }
}