using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.Mappings;
using ReportService.Domain.Entities;
using ReportService.Domain.Events;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ReportService.Application.UseCases.GenerateReport;

public sealed class GenerateReportHandler
{
    private readonly GenerateReportValidator _validator;
    private readonly IAnalysisReportRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IReportRenderer _reportRenderer;
    private readonly IReportStorage _reportStorage;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<ReportGeneratedDomainEvent> _generatedEventMapper;
    private readonly ActivitySource _activitySource;

    public GenerateReportHandler(
        GenerateReportValidator validator,
        IAnalysisReportRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IReportRenderer reportRenderer,
        IReportStorage reportStorage,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<ReportGeneratedDomainEvent> generatedEventMapper,
        ActivitySource activitySource)
    {
        _validator = validator;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _reportRenderer = reportRenderer;
        _reportStorage = reportStorage;
        _eventPublisher = eventPublisher;
        _generatedEventMapper = generatedEventMapper;
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
        activity?.SetTag("report.format", command.Format);

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

        var existing = await _repository.GetByAnalysisRequestIdAsync(
            command.AnalysisRequestId,
            cancellationToken);

        if (existing is not null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "A report for the analysis request already exists.");
            activity?.SetTag("exception.type", typeof(InvalidOperationException).FullName);
            activity?.SetTag("exception.message", $"A report for analysis request '{command.AnalysisRequestId}' already exists.");

            return Result.Failure<GenerateReportResult>(
                Error.Conflict(
                    "report.already_exists",
                    $"A report for analysis request '{command.AnalysisRequestId}' already exists."));
        }

        var now = _dateTimeProvider.UtcNow;
        var reportId = Guid.NewGuid();

        try
        {
            var rawContent = AnalysisReportMappings.ToMarkdownDocument(
                command.AnalysisRequestId,
                command.RequestedByUserId,
                command.Result);

            var rendered = await _reportRenderer.RenderAsync(
                new RenderReportRequest(
                    command.AnalysisRequestId,
                    command.RequestedByUserId,
                    command.Format,
                    $"analysis-report-{command.AnalysisRequestId:N}",
                    rawContent),
                cancellationToken);

            var stored = await _reportStorage.UploadAsync(
                new UploadReportRequest(
                    rendered.FileName,
                    rendered.ContentType,
                    rendered.Content),
                cancellationToken);

            var report = AnalysisReport.Create(
                id: reportId,
                analysisRequestId: command.AnalysisRequestId,
                requestedByUserId: command.RequestedByUserId,
                format: command.Format,
                content: rawContent,
                bucketName: stored.BucketName,
                objectKey: stored.ObjectKey,
                fileName: stored.FileName,
                contentType: stored.ContentType,
                createdAtUtc: now);

            await _repository.AddAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var domainEvents = report.DequeueDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                if (domainEvent is ReportGeneratedDomainEvent generatedDomainEvent)
                {
                    var integrationEvent = _generatedEventMapper.Map(generatedDomainEvent);
                    await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
                }
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new GenerateReportResult(
                report.Id,
                report.AnalysisRequestId,
                report.Status,
                report.Format,
                report.FileName,
                report.GeneratedFileLocation.BucketName,
                report.GeneratedFileLocation.ObjectKey,
                report.GeneratedAtUtc ?? report.CreatedAtUtc));
        }
        catch(Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            var errorCode = "report.error";

            if (ex is DomainException)
            {
                errorCode = "report.domain_error";

                return Result.Failure<GenerateReportResult>(
                    Error.Failure(errorCode, ex.Message));
            }
            else if (ex is ValidationException)
                errorCode = "report.validation_error";
            else if (ex is ArgumentException)
                errorCode = "report.invalid_argument";

            return Result.Failure<GenerateReportResult>(
                Error.Validation(errorCode, ex.Message));
        }
    }
}