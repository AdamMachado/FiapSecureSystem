using Shared.Kernel.Result;
using System.Diagnostics;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.Abstractions.Storage;

namespace UploadService.Application.UseCases.GetAnalysisAsset;

public sealed class GetAnalysisAssetHandler
{
    private readonly IUserContext _userContext;
    private readonly IAnalysisRequestRepository _repository;
    private readonly IObjectStorage _objectStorage;
    private readonly ActivitySource _activitySource;

    public GetAnalysisAssetHandler(
        IUserContext userContext,
        IAnalysisRequestRepository repository,
        IObjectStorage objectStorage,
        ActivitySource activitySource)
    {
        _userContext = userContext;
        _repository = repository;
        _objectStorage = objectStorage;
        _activitySource = activitySource;
    }

    public async Task<Result<GetAnalysisAssetResult>> HandleAsync(
        GetAnalysisAssetQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        using var activity = _activitySource.StartActivity(
            "UploadService get analysis asset",
            ActivityKind.Internal);

        var userId = _userContext.GetRequiredUserId();

        activity?.SetTag("user.id", userId);
        activity?.SetTag("analysisRequestId", query.AnalysisRequestId);

        var analysisRequest = await _repository.GetByIdForUserAsync(
            query.AnalysisRequestId,
            userId,
            cancellationToken);

        if (analysisRequest is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Analysis request not found.");

            return Result.Failure<GetAnalysisAssetResult>(Error.NotFound(
                "analysis_request.not_found",
                $"Analysis request with Id '{query.AnalysisRequestId}' was not found."));
        }

        var objectContent = await _objectStorage.DownloadAsync(
            new DownloadObjectRequest(
                analysisRequest.StorageLocation.BucketName,
                analysisRequest.StorageLocation.ObjectKey),
            cancellationToken);

        var contentType = string.IsNullOrWhiteSpace(objectContent.ContentType)
            ? analysisRequest.FileMetadata.ContentType
            : objectContent.ContentType;

        activity?.SetTag("storage.bucket_name", analysisRequest.StorageLocation.BucketName);
        activity?.SetTag("storage.object_key", analysisRequest.StorageLocation.ObjectKey);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Success(new GetAnalysisAssetResult(
            objectContent.Content,
            contentType,
            analysisRequest.FileMetadata.FileName,
            objectContent.SizeInBytes));
    }
}
