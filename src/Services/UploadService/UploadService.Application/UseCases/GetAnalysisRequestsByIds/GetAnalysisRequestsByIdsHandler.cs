using Shared.Kernel.Result;
using System.Diagnostics;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.UseCases.Common;

namespace UploadService.Application.UseCases.GetAnalysisRequestsByIds;

public sealed class GetAnalysisRequestsByIdsHandler
{
    private readonly IUserContext _userContext;
    private readonly IAnalysisRequestRepository _repository;
    private readonly ActivitySource _activitySource;

    public GetAnalysisRequestsByIdsHandler(
        IUserContext userContext,
        IAnalysisRequestRepository repository,
        ActivitySource activitySource)
    {
        _userContext = userContext;
        _repository = repository;
        _activitySource = activitySource;
    }

    public async Task<Result<IReadOnlyCollection<AnalysisRequestSummaryResult>>> HandleAsync(
        GetAnalysisRequestsByIdsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AnalysisRequestIds);

        using var activity = _activitySource.StartActivity(
            "UploadService get analysis requests by ids",
            ActivityKind.Internal);

        var userId = _userContext.GetRequiredUserId();

        activity?.SetTag("user.id", userId);
        activity?.SetTag("analysis_requests.requested_count", query.AnalysisRequestIds.Count);

        var analysisRequests = await _repository.ListByUserAndIdsAsync(
            userId,
            query.AnalysisRequestIds,
            cancellationToken);

        var summariesById = analysisRequests
            .Select(x => x.ToSummaryResult())
            .ToDictionary(x => x.AnalysisRequestId);

        var response = query.AnalysisRequestIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Where(summariesById.ContainsKey)
            .Select(id => summariesById[id])
            .ToArray();

        activity?.SetTag("analysis_requests.found_count", response.Length);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Success<IReadOnlyCollection<AnalysisRequestSummaryResult>>(response);
    }
}
