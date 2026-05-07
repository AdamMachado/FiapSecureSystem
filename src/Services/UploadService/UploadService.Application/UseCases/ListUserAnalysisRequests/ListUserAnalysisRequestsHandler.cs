using Shared.Kernel.Pagination;
using Shared.Kernel.Result;
using System.Diagnostics;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.UseCases.Common;

namespace UploadService.Application.UseCases.ListUserAnalysisRequests;

public sealed class ListUserAnalysisRequestsHandler
{
    private readonly IUserContext _userContext;
    private readonly IAnalysisRequestRepository _repository;
    private readonly ActivitySource _activitySource;

    public ListUserAnalysisRequestsHandler(
        IUserContext userContext,
        IAnalysisRequestRepository repository,
        ActivitySource activitySource)
    {
        _userContext = userContext;
        _repository = repository;
        _activitySource = activitySource;
    }

    public async Task<Result<PagedResult<AnalysisRequestSummaryResult>>> HandleAsync(
        ListUserAnalysisRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.PaginationParams);

        using var activity = _activitySource.StartActivity(
            "UploadService list user analysis requests",
            ActivityKind.Internal);

        var userId = _userContext.GetRequiredUserId();

        activity?.SetTag("user.id", userId);
        activity?.SetTag("pagination.page_number", query.PaginationParams.PageNumber);
        activity?.SetTag("pagination.page_size", query.PaginationParams.PageSize);

        var pagedAnalysisRequests = await _repository.ListByUserAsync(
            userId,
            query.PaginationParams,
            cancellationToken);

        var response = PagedResult<AnalysisRequestSummaryResult>.Create(
            pagedAnalysisRequests.Items
                .Select(x => x.ToSummaryResult())
                .ToArray(),
            pagedAnalysisRequests.TotalCount,
            pagedAnalysisRequests.PageNumber,
            pagedAnalysisRequests.PageSize);

        activity?.SetTag("analysis_requests.count", response.Items.Count);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return Result.Success(response);
    }
}
