using Microsoft.Extensions.Logging;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Kernel.Result;

namespace ProcessingService.Application.UseCases.FailAnalysisProcessing;

public sealed class FailAnalysisProcessingHandler
{
    private readonly IAnalysisProcessRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent> _integrationEventMapper;

    private readonly ILogger<FailAnalysisProcessingHandler> _logger;

    public FailAnalysisProcessingHandler(
        IAnalysisProcessRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent> integrationEventMapper,
        ILogger<FailAnalysisProcessingHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;

        _eventPublisher = eventPublisher;
        _integrationEventMapper = integrationEventMapper;

        _logger = logger;
    }

    public async Task<Result<FailAnalysisProcessingResult>> HandleAsync(
        FailAnalysisProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling {CommandType} for AnalysisRequestId={AnalysisRequestId}",
            nameof(FailAnalysisProcessingCommand),
            command.AnalysisRequestId);

        var process = await _repository.GetByAnalysisRequestIdAsync(
            AnalysisRequestId.Create(command.AnalysisRequestId), 
            cancellationToken);

        if (process is null)
        {
            _logger.LogWarning(
                "No analysis process found for AnalysisRequestId={AnalysisRequestId}. Cannot mark processing as failed.",
                command.AnalysisRequestId);

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

        _logger.LogInformation(
            "Marked analysis processing as failed. AnalysisProcessId={AnalysisProcessId}, AnalysisRequestId={AnalysisRequestId}, Reason={Reason}",
            process.Id,
            process.AnalysisRequestId,
            command.Reason);

        return Result.Success(new FailAnalysisProcessingResult(
            process.Id,
            process.AnalysisRequestId.Value,
            process.Status,
            failedAtUtc,
            process.FailureReason!,
            process.FailureDetails));
    }
}
