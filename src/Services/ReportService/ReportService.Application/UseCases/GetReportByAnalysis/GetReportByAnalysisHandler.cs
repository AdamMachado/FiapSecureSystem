using ReportService.Application.Abstractions.Persistence;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ReportService.Application.UseCases.GetReportByAnalysis;

public sealed class GetReportByAnalysisHandler
{
    private readonly IAnalysisReportRepository _repository;
    private readonly ActivitySource _activitySource;

    public GetReportByAnalysisHandler(IAnalysisReportRepository repository, ActivitySource activitySource)
    {
        _repository = repository;
        _activitySource = activitySource;
    }

    public async Task<Result<GetReportByAnalysisResult>> HandleAsync(
        GetReportByAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ReportService get report by analysis",
            ActivityKind.Internal);

        activity?.SetTag("report.analysis_request.id", query.AnalysisRequestId);

        try
        {
            var report = await _repository.GetByAnalysisRequestIdAsync(
                query.AnalysisRequestId,
                cancellationToken);

            if (report is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Report not found for analysis request.");
                activity?.SetTag("exception.type", typeof(InvalidOperationException).FullName);
                activity?.SetTag("exception.message", $"No report found for analysis request '{query.AnalysisRequestId}'.");

                return Result.Failure<GetReportByAnalysisResult>(
                    Error.NotFound(
                        "report.not_found",
                        $"No report found for analysis request '{query.AnalysisRequestId}'."));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new GetReportByAnalysisResult(
                report.Id,
                report.AnalysisRequestId,
                report.RequestedByUserId,
                report.Status,
                report.Format,
                report.FileName,
                report.ContentType,
                report.GeneratedFileLocation.BucketName,
                report.GeneratedFileLocation.ObjectKey,
                report.CreatedAtUtc,
                report.UpdatedAtUtc,
                report.GeneratedAtUtc,
                report.FailureReason));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure<GetReportByAnalysisResult>(
                Error.Failure(
                    "report.retrieval_failed",
                    $"An error occurred while retrieving the report for analysis request '{query.AnalysisRequestId}'."));
        }
    }
}