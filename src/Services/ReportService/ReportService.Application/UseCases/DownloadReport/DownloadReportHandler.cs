using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Storage;
using ReportService.Domain;
using ReportService.Domain.Enums;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ReportService.Application.UseCases.DownloadReport;

public sealed class DownloadReportHandler
{
    private readonly IAnalysisReportRepository _repository;
    private readonly IReportStorage _reportStorage;
    private readonly ActivitySource _activitySource;

    public DownloadReportHandler(
        IAnalysisReportRepository repository,
        IReportStorage reportStorage, 
        ActivitySource activitySource)
    {
        _repository = repository;
        _reportStorage = reportStorage;
        _activitySource = activitySource;
    }

    public async Task<Result<DownloadReportResult>> HandleAsync(
        DownloadReportQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ReportService download report",
            ActivityKind.Internal);

        activity?.SetTag("report.analysis_request.id", query.AnalysisRequestId);

        try
        {
            var report = await _repository.GetByAnalysisRequestIdAsync(
                query.AnalysisRequestId,
                cancellationToken);

            if (report is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "No report found for the analysis request.");
                activity?.SetTag("exception.type", typeof(InvalidOperationException).FullName);
                activity?.SetTag("exception.message", $"No report found for analysis request '{query.AnalysisRequestId}'.");

                return Result.Failure<DownloadReportResult>(
                    Error.NotFound(
                        "report.not_found",
                        $"No report found for analysis request '{query.AnalysisRequestId}'."));
            }

            if (report.Status != ReportStatus.Generated)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Report is not available for download.");
                activity?.SetTag("exception.type", typeof(InvalidOperationException).FullName);
                activity?.SetTag("exception.message", $"Report for analysis request '{query.AnalysisRequestId}' is not available for download.");

                return Result.Failure<DownloadReportResult>(
                    Error.Conflict(
                        "report.not_available",
                        $"Report for analysis request '{query.AnalysisRequestId}' is not available for download."));
            }

            var downloaded = await _reportStorage.DownloadAsync(
                report.GeneratedFileLocation.BucketName,
                report.GeneratedFileLocation.ObjectKey,
                cancellationToken);

            if (downloaded is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Report file not found in storage.");
                activity?.SetTag("exception.type", typeof(FileNotFoundException).FullName);
                activity?.SetTag("exception.message", $"Report file for analysis request '{query.AnalysisRequestId}' was not found in storage.");

                return Result.Failure<DownloadReportResult>(
                    Error.NotFound(
                        "report.file_not_found",
                        "Report file was not found in storage."));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new DownloadReportResult(
                report.Id,
                report.AnalysisRequestId,
                downloaded.FileName,
                downloaded.ContentType,
                downloaded.Content));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure<DownloadReportResult>(
                Error.Failure(
                    "report.download_failed",
                    $"An error occurred while downloading the report: {ex.Message}"));
        }
    }
}