using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Result;
using System.Diagnostics;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.Abstractions.Storage;
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
    private readonly IStorageObjectKeyFactory _storageObjectKeyFactory;
    private readonly IUploadPolicy _uploadPolicy;
    private readonly IAnalysisRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIntegrationEventMapper<AnalysisRequestCreatedDomainEvent> _integrationEventMapper;
    private readonly ActivitySource _activitySource;

    public CreateAnalysisHandler(
        CreateAnalysisValidator validator,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        IObjectStorage objectStorage,
        IStorageObjectKeyFactory storageObjectKeyFactory,
        IUploadPolicy uploadPolicy,
        IAnalysisRequestRepository repository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IIntegrationEventMapper<AnalysisRequestCreatedDomainEvent> integrationEventMapper,
        ActivitySource activitySource)
    {
        _validator = validator;
        _userContext = userContext;
        _dateTimeProvider = dateTimeProvider;
        _objectStorage = objectStorage;
        _storageObjectKeyFactory = storageObjectKeyFactory;
        _uploadPolicy = uploadPolicy;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _integrationEventMapper = integrationEventMapper;
        _activitySource = activitySource;
    }

    public async Task<Result<CreateAnalysisResult>> HandleAsync(
        CreateAnalysisCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "UploadService create analysis",
            ActivityKind.Internal);

        activity?.SetTag("upload.file.name", command.FileName);
        activity?.SetTag("upload.file.content_type", command.ContentType);
        activity?.SetTag("upload.file.size_bytes", command.SizeInBytes);

        try
        {
            _validator.ValidateAndThrow(command);
        }
        catch (ArgumentException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure<CreateAnalysisResult>(
                Error.Validation("analysis.validation_error", ex.Message));
        }

        var now = _dateTimeProvider.UtcNow;
        var userId = _userContext.GetRequiredUserId();
        var analysisRequestId = Guid.NewGuid();

        if (!_uploadPolicy.IsContentTypeSupported(command.ContentType))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Unsupported content type.");
            activity?.SetTag("exception.type", typeof(UnsupportedContentTypeException).FullName);
            activity?.SetTag("exception.message", $"Unsupported content type '{command.ContentType}'.");

            return Result.Failure<CreateAnalysisResult>(
                Error.Validation(
                    "analysis.unsupported_content_type",
                    $"Unsupported content type '{command.ContentType}'."));
        }

        var fileType = _uploadPolicy.ResolveFileType(command.ContentType);

        var objectKey = _storageObjectKeyFactory.CreateForAnalysisUpload(
            analysisRequestId,
            command.FileName,
            now);

        var uploadResult = await _objectStorage.UploadAsync(
            new UploadObjectRequest(
                objectKey,
                command.Content,
                command.ContentType),
            cancellationToken);

        try
        {
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
                storageLocation: uploadResult.Location,
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

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new CreateAnalysisResult(
                analysisRequest.Id,
                analysisRequest.Status,
                analysisRequest.CreatedAtUtc));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            var errorCode = "analysis.error";

            if(ex is ValidationException)
                errorCode = "analysis.validation_error";
            else if (ex is DomainException)
                errorCode = "analysis.domain_error";
            else if (ex is ArgumentException)
                errorCode = "analysis.invalid_argument";

            return Result.Failure<CreateAnalysisResult>(
                Error.Validation(errorCode, ex.Message));
        }
    }
}