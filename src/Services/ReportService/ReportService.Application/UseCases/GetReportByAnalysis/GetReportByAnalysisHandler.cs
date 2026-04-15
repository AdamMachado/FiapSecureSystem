using ReportService.Application.Abstractions.Persistence;
using Shared.Kernel.Result;

namespace ReportService.Application.UseCases.GetReportByAnalysis;

public sealed class GetReportByAnalysisHandler
{
    private readonly IAnalysisReportRepository _repository;

    public GetReportByAnalysisHandler(IAnalysisReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetReportByAnalysisResult>> HandleAsync(
        GetReportByAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var report = await _repository.GetByAnalysisRequestIdAsync(
            query.AnalysisRequestId,
            cancellationToken);

        if (report is null)
        {
            return Result.Failure<GetReportByAnalysisResult>(
                Error.NotFound(
                    "report.not_found",
                    $"No report found for analysis request '{query.AnalysisRequestId}'."));
        }

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
}