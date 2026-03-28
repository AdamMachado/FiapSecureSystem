using FiapSecureSystem.UploadOrchestration.Application.Abstractions;
using FiapSecureSystem.UploadOrchestration.Application.DTOs;
using FiapSecureSystem.UploadOrchestration.Domain.Entities;

namespace FiapSecureSystem.UploadOrchestration.Application.UseCases;

public class CreateAnalysisRequestUseCase
{
    private readonly IAnalysisRequestRepository _repository;
    private readonly IFileStorage _fileStorage;
    private readonly IMessageBus _messageBus;

    public CreateAnalysisRequestUseCase(
        IAnalysisRequestRepository repository,
        IFileStorage fileStorage,
        IMessageBus messageBus)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _messageBus = messageBus;
    }

    public async Task<CreateAnalysisRequestOutput> ExecuteAsync(
        CreateAnalysisRequestInput input,
        CancellationToken cancellationToken)
    {
        var storagePath = await _fileStorage.SaveAsync(
            input.FileStream,
            input.FileName,
            cancellationToken);

        var request = new AnalysisRequest(
            input.FileName,
            input.ContentType,
            storagePath,
            input.CorrelationId);

        await _repository.AddAsync(request, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var message = new
        {
            AnalysisRequestId = request.Id,
            request.FileName,
            request.ContentType,
            request.StoragePath,
            request.CorrelationId,
            RequestedAt = DateTime.UtcNow
        };

        await _messageBus.PublishAnalysisRequestedAsync(message, cancellationToken);

        return new CreateAnalysisRequestOutput(request.Id, request.Status.ToString());
    }
}