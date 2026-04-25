using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Domain.Events;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.FailAnalysisProcessing;

public sealed class FailAnalysisProcessingHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent> _integrationEventMapper;

    public FailAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent> integrationEventMapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;

        _eventPublisher = eventPublisher;
        _integrationEventMapper = integrationEventMapper;
    }

    public async Task<Result<FailAnalysisProcessingResult>> HandleAsync(
        FailAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        var process = await _repository.GetByAnalysisRequestIdAsync(command.AnalysisRequestId, cancellationToken);
        if (process is null)
        {
            return Result.Failure<FailAnalysisProcessingResult>(
                Error.NotFound(
                    "processing.not_found",
                    $"Analysis process for request '{command.AnalysisRequestId}' was not found."));
        }

        var failedAtUtc = _dateTimeProvider.UtcNow;
        process.MarkAsFailed(command.Reason, command.Details, failedAtUtc);

        _repository.Update(process);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var domainEvent = process
            .DequeueDomainEvents()
            .OfType<AnalysisProcessingFailedDomainEvent>()
            .Single();

        await _eventPublisher.PublishAsync(_integrationEventMapper.Map(domainEvent), cancellationToken);

        return Result.Success(new FailAnalysisProcessingResult(
            process.Id,
            process.AnalysisRequestId.Value,
            process.Status,
            failedAtUtc,
            process.FailureReason!,
            process.FailureDetails));
    }
}
