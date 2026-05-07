using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.UseCases.GenerateReportFile;
using Shared.Kernel.Result;
using System.Diagnostics;

namespace ReportService.Application.UseCases.DownloadReportFile;

public sealed class DownloadReportFileHandler
{
    private readonly IAnalysisReportRepository _repository;
    private readonly IReportStorage _reportStorage;
    private readonly GenerateReportFileHandler _generateReportFileHandler;
    private readonly ActivitySource _activitySource;

    public DownloadReportFileHandler(
        IAnalysisReportRepository repository,
        IReportStorage reportStorage,
        GenerateReportFileHandler generateReportFileHandler,
        ActivitySource activitySource)
    {
        _repository = repository;
        _reportStorage = reportStorage;
        _generateReportFileHandler = generateReportFileHandler;
        _activitySource = activitySource;
    }

    public async Task<Result<DownloadReportFileResult>> HandleAsync(
        DownloadReportFileQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            "ReportService download report file",
            ActivityKind.Internal);

        activity?.SetTag("report.analysis_request.id", query.AnalysisRequestId);
        activity?.SetTag("report.format", query.Format.ToString());

        try
        {
            var report = await _repository.GetByAnalysisRequestIdAsync(
                query.AnalysisRequestId,
                cancellationToken);

            if (report is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "No report found for the analysis request.");

                return Result.Failure<DownloadReportFileResult>(
                    Error.NotFound(
                        "report.not_found",
                        $"No report found for analysis request '{query.AnalysisRequestId}'."));
            }

            var reportFile = report.GetFile(query.Format);

            if (reportFile is null)
            {
                var generationResult = await _generateReportFileHandler.HandleAsync(
                    new GenerateReportFileCommand(query.AnalysisRequestId, query.Format),
                    cancellationToken);

                if (generationResult.IsFailure)
                    return Result.Failure<DownloadReportFileResult>(generationResult.Error);

                report = await _repository.GetByAnalysisRequestIdAsync(
                    query.AnalysisRequestId,
                    cancellationToken);

                reportFile = report?.GetFile(query.Format);

                if (reportFile is null)
                {
                    return Result.Failure<DownloadReportFileResult>(
                        Error.Failure(
                            "report.file_generation_inconsistent",
                            "The report file was generated but not found in persistence."));
                }
            }

            var downloaded = await _reportStorage.DownloadAsync(
                reportFile.BucketName,
                reportFile.ObjectKey,
                cancellationToken);

            if (downloaded is null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Report file not found in storage.");

                return Result.Failure<DownloadReportFileResult>(
                    Error.NotFound(
                        "report.file_not_found",
                        "Report file was not found in storage."));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Result.Success(new DownloadReportFileResult(
                report!.Id,
                report.AnalysisRequestId,
                reportFile.Format,
                downloaded.FileName,
                downloaded.ContentType,
                downloaded.Content));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);

            return Result.Failure<DownloadReportFileResult>(
                Error.Failure(
                    "report.download_failed",
                    $"An error occurred while downloading the report: {ex.Message}"));
        }
    }
}
