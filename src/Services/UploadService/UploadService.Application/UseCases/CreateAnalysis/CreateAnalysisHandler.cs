using UploadService.Application.Abstractions.Clock;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.Abstractions.Storage;
using UploadService.Application.Integration.Published;
using UploadService.Domain.Entities;
using UploadService.Domain.Events;
using UploadService.Domain.ValueObjects;

namespace UploadService.Application.UseCases.CreateAnalysis;

public sealed class CreateAnalysisHandler
{
    private readonly CreateAnalysisValidator _validator;
    private readonly IUserContext _userContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IObjectStorage _objectStorage;
    private readonly IAnalysisRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisRequestCreatedDomainEvent> _integrationEventMapper;

    public CreateAnalysisHandler(
        CreateAnalysisValidator validator,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        IObjectStorage objectStorage,
        IAnalysisRequestRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        AnalysisRequestedIntegrationEventMapper integrationEventMapper)
    {
        _validator = validator;
        _userContext = userContext;
        _dateTimeProvider = dateTimeProvider;
        _objectStorage = objectStorage;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _integrationEventMapper = integrationEventMapper;
    }

    public async Task<CreateAnalysisResult> HandleAsync(
        CreateAnalysisCommand command,
        CancellationToken cancellationToken = default)
    {
        _validator.ValidateAndThrow(command);

        var now = _dateTimeProvider.UtcNow;
        var userId = _userContext.GetRequiredUserId();
        var analysisRequestId = Guid.NewGuid();

        var fileType = CreateAnalysisValidator.ResolveFileType(command.ContentType);
        var storageObjectKey = StorageObjectKey.Create(
            $"uploads/{now:yyyy/MM/dd}/{analysisRequestId:N}-{SanitizeFileName(command.FileName)}");

        var uploadResult = await _objectStorage.UploadAsync(
            new UploadObjectRequest(
                storageObjectKey.Value,
                command.Content,
                command.ContentType),
            cancellationToken);

        var fileMetadata = FileMetadata.Create(
            command.FileName,
            command.ContentType,
            command.SizeInBytes,
            fileType);

        var fileHash = FileHash.Create(command.FileHash);

        var analysisRequest = AnalysisRequest.Create(
            id: analysisRequestId,
            requestedByUserId: userId,
            fileMetadata: fileMetadata,
            fileHash: fileHash,
            storageObjectKey: StorageObjectKey.Create(uploadResult.ObjectKey),
            createdAtUtc: now);

        await _repository.AddAsync(analysisRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var domainEvents = analysisRequest.DequeueDomainEvents();

        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is AnalysisRequestCreatedDomainEvent createdDomainEvent)
            {
                var integrationEvent = _integrationEventMapper.Map(createdDomainEvent);
                await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            }
        }

        return new CreateAnalysisResult(
            analysisRequest.Id,
            analysisRequest.Status,
            analysisRequest.CreatedAtUtc);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return sanitized.Replace(' ', '-');
    }
}