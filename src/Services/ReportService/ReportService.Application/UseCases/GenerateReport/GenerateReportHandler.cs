using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Mappings;
using ReportService.Domain.Entities;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ReportService.Application.UseCases.GenerateReport;

public sealed class GenerateReportHandler
{
    private readonly GenerateReportValidator _validator;
    private readonly IAnalysisReportRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ActivitySource _activitySource;

    public GenerateReportHandler(
        GenerateReportValidator validator,
        IAnalysisReportRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ActivitySource activitySource)
    {
        _validator = validator;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _activitySource = activitySource;
    }

    public async Task<Result<GenerateReportResult>> HandleAsync(
        GenerateReportCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ReportService generate report",
            ActivityKind.Internal);

        activity?.SetTag("report.analysis_request.id", command.AnalysisRequestId);

        try
        {
            _validator.ValidateAndThrow(command);
        }
        catch (ArgumentException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure<GenerateReportResult>(
                Error.Validation("report.validation_error", ex.Message));
        }

        try
        {
            var existing = await _repository.GetByAnalysisRequestIdAsync(
                command.AnalysisRequestId,
                cancellationToken);

            if (existing is not null)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);

                return Result.Success(new GenerateReportResult(
                    existing.Id,
                    existing.AnalysisRequestId,
                    existing.RequestedByUserId,
                    existing.CreatedAtUtc,
                    existing.UpdatedAtUtc));
            }

            var report = AnalysisReport.Create(
                id: Guid.NewGuid(),
                analysisRequestId: command.AnalysisRequestId,
                requestedByUserId: command.RequestedByUserId,
                analysisData: AnalysisReportMappings.ToAnalysisJson(command.Result),
                createdAtUtc: _dateTimeProvider.UtcNow);

            await _repository.AddAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new GenerateReportResult(
                report.Id,
                report.AnalysisRequestId,
                report.RequestedByUserId,
                report.CreatedAtUtc,
                report.UpdatedAtUtc));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure<GenerateReportResult>(
                Error.Failure("report.error", ex.Message));
        }
    }
}
