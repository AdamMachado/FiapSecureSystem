using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Storage;
using ReportService.Domain.Enums;
using Shared.Kernel.Result;

namespace ReportService.Application.UseCases.DownloadReport;

public sealed class DownloadReportHandler
{
    private readonly IAnalysisReportRepository _repository;
    private readonly IReportStorage _reportStorage;

    public DownloadReportHandler(
        IAnalysisReportRepository repository,
        IReportStorage reportStorage)
    {
        _repository = repository;
        _reportStorage = reportStorage;
    }

    public async Task<Result<DownloadReportResult>> HandleAsync(
        DownloadReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var report = await _repository.GetByAnalysisRequestIdAsync(
            query.AnalysisRequestId,
            cancellationToken);

        if (report is null)
        {
            return Result.Failure<DownloadReportResult>(
                Error.NotFound(
                    "report.not_found",
                    $"No report found for analysis request '{query.AnalysisRequestId}'."));
        }

        if (report.Status != ReportStatus.Generated)
        {
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
            return Result.Failure<DownloadReportResult>(
                Error.NotFound(
                    "report.file_not_found",
                    "Report file was not found in storage."));
        }

        return Result.Success(new DownloadReportResult(
            report.Id,
            report.AnalysisRequestId,
            downloaded.FileName,
            downloaded.ContentType,
            downloaded.Content));
    }
}