using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.Mappings;
using ReportService.Domain.Events;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ReportService.Application.UseCases.GenerateReportFile;

public sealed class GenerateReportFileHandler
{
    private readonly IAnalysisReportRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IReadOnlyCollection<IReportRenderer> _reportRenderers;
    private readonly IReportStorage _reportStorage;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<ReportGeneratedDomainEvent> _generatedEventMapper;
    private readonly ActivitySource _activitySource;

    public GenerateReportFileHandler(
        IAnalysisReportRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEnumerable<IReportRenderer> reportRenderers,
        IReportStorage reportStorage,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<ReportGeneratedDomainEvent> generatedEventMapper,
        ActivitySource activitySource)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _reportRenderers = reportRenderers.ToArray();
        _reportStorage = reportStorage;
        _eventPublisher = eventPublisher;
        _generatedEventMapper = generatedEventMapper;
        _activitySource = activitySource;
    }

    public async Task<Result<GenerateReportFileResult>> HandleAsync(
        GenerateReportFileCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ReportService generate report file",
            ActivityKind.Internal);

        activity?.SetTag("report.analysis_request.id", command.AnalysisRequestId);
        activity?.SetTag("report.format", command.Format.ToString());

        if (command.AnalysisRequestId == Guid.Empty)
        {
            return Result.Failure<GenerateReportFileResult>(
                Error.Validation("report.validation_error", "AnalysisRequestId is required."));
        }

        if (!Enum.IsDefined(command.Format))
        {
            return Result.Failure<GenerateReportFileResult>(
                Error.Validation("report.validation_error", "Invalid report format."));
        }

        try
        {
            var report = await _repository.GetByAnalysisRequestIdAsync(
                command.AnalysisRequestId,
                cancellationToken);

            if (report is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Report not found for analysis request.");

                return Result.Failure<GenerateReportFileResult>(
                    Error.NotFound(
                        "report.not_found",
                        $"No report found for analysis request '{command.AnalysisRequestId}'."));
            }

            var existingFile = report.GetFile(command.Format);

            if (existingFile is not null)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);

                return Result.Success(new GenerateReportFileResult(
                    report.Id,
                    report.AnalysisRequestId,
                    existingFile.Format,
                    existingFile.FileName,
                    existingFile.ContentType,
                    existingFile.BucketName,
                    existingFile.ObjectKey,
                    existingFile.CreatedAtUtc));
            }

            var renderer = _reportRenderers.FirstOrDefault(x => x.CanRender(command.Format));

            if (renderer is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Renderer not configured for requested format.");

                return Result.Failure<GenerateReportFileResult>(
                    Error.Validation(
                        "report.unsupported_format",
                        $"No renderer configured for format '{command.Format}'."));
            }

            var analysisResult = AnalysisReportMappings.FromAnalysisJson(report.AnalysisData);

            var rendered = await renderer.RenderAsync(
                new RenderReportRequest(
                    report.AnalysisRequestId,
                    report.RequestedByUserId,
                    command.Format,
                    $"analysis-report-{report.AnalysisRequestId:N}",
                    analysisResult),
                cancellationToken);

            var stored = await _reportStorage.UploadAsync(
                new UploadReportRequest(
                    rendered.FileName,
                    rendered.ContentType,
                    rendered.Content),
                cancellationToken);

            var createdFile = report.AddFile(
                Guid.NewGuid(),
                command.Format,
                stored.BucketName,
                stored.ObjectKey,
                stored.FileName,
                stored.ContentType,
                _dateTimeProvider.UtcNow);

            _repository.Update(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var domainEvent in report.DequeueDomainEvents().OfType<ReportGeneratedDomainEvent>())
            {
                var integrationEvent = _generatedEventMapper.Map(domainEvent);
                await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new GenerateReportFileResult(
                report.Id,
                report.AnalysisRequestId,
                createdFile.Format,
                createdFile.FileName,
                createdFile.ContentType,
                createdFile.BucketName,
                createdFile.ObjectKey,
                createdFile.CreatedAtUtc));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure<GenerateReportFileResult>(
                Error.Failure("report.file_generation_failed", ex.Message));
        }
    }
}
