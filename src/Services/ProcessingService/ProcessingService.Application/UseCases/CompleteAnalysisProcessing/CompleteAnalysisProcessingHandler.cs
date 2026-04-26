using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.CompleteAnalysisProcessing;

public sealed class CompleteAnalysisProcessingHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent> _integrationEventMapper;

    public CompleteAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent> integrationEventMapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;

        _eventPublisher = eventPublisher;
        _integrationEventMapper = integrationEventMapper;
    }

    public async Task<Result<CompleteAnalysisProcessingResult>> HandleAsync(
        CompleteAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        var process = await _repository.GetByAnalysisRequestIdAsync(
            AnalysisRequestId.Create(command.AnalysisRequestId), 
            cancellationToken);

        if (process is null)
        {
            return Result.Failure<CompleteAnalysisProcessingResult>(
                Error.NotFound(
                    "processing.not_found",
                    $"Analysis process for request '{command.AnalysisRequestId}' was not found."));
        }

        var summary = ProcessingResultSummary.Create(
            command.Overview,
            command.Components.Count,
            command.Risks.Count,
            command.Recommendations.Count,
            command.RequiresManualReview,
            command.Warnings);

        var completedAtUtc = _dateTimeProvider.UtcNow;

        process.MarkAsCompleted(
            ExtractedText.Create(command.ExtractedText),
            command.Components,
            command.Risks,
            command.Recommendations,
            summary,
            completedAtUtc);

        _repository.Update(process);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var domainEvent = process
            .DequeueDomainEvents()
            .OfType<AnalysisProcessingCompletedDomainEvent>()
            .Single();

        await _eventPublisher.PublishAsync(_integrationEventMapper.Map(domainEvent), cancellationToken);

        return Result.Success(new CompleteAnalysisProcessingResult(
            process.Id,
            process.AnalysisRequestId.Value,
            process.Status,
            completedAtUtc,
            process.Components.Count,
            process.Risks.Count,
            process.Recommendations.Count));
    }
}
