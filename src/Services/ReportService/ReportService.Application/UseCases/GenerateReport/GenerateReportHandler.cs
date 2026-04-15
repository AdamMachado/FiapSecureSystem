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

    public GenerateReportHandler(
        GenerateReportValidator validator,
        IAnalysisReportRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IReportRenderer reportRenderer,
        IReportStorage reportStorage,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<ReportGeneratedDomainEvent> generatedEventMapper)
    {
        _validator = validator;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _reportRenderer = reportRenderer;
        _reportStorage = reportStorage;
        _eventPublisher = eventPublisher;
        _generatedEventMapper = generatedEventMapper;
    }

    public async Task<Result<GenerateReportResult>> HandleAsync(
        GenerateReportCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _validator.ValidateAndThrow(command);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<GenerateReportResult>(
                Error.Validation("report.validation_error", ex.Message));
        }

        var existing = await _repository.GetByAnalysisRequestIdAsync(
            command.AnalysisRequestId,
            cancellationToken);

        if (existing is not null)
        {
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
        catch (ValidationException ex)
        {
            return Result.Failure<GenerateReportResult>(
                Error.Validation("report.validation_error", ex.Message));
        }
        catch (DomainException ex)
        {
            return Result.Failure<GenerateReportResult>(
                Error.Failure("report.domain_error", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<GenerateReportResult>(
                Error.Validation("report.invalid_argument", ex.Message));
        }
    }
}