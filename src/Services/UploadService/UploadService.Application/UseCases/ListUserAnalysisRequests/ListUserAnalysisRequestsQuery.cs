using Shared.Kernel.Pagination;

namespace UploadService.Application.UseCases.ListUserAnalysisRequests;

public sealed record ListUserAnalysisRequestsQuery(PaginationParams PaginationParams);
