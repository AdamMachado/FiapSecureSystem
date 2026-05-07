namespace ReportService.Application.UseCases.GenerateReport;

public sealed class GenerateReportValidator
{
    public void ValidateAndThrow(GenerateReportCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.AnalysisRequestId == Guid.Empty)
            throw new ArgumentException("AnalysisRequestId is required.", nameof(command.AnalysisRequestId));

        if (command.RequestedByUserId == Guid.Empty)
            throw new ArgumentException("RequestedByUserId is required.", nameof(command.RequestedByUserId));

        if (command.Result is null)
            throw new ArgumentException("Analysis result is required.", nameof(command.Result));

        if (command.Result.Summary is null)
            throw new ArgumentException("Analysis summary is required.", nameof(command.Result));

        if (string.IsNullOrWhiteSpace(command.Result.Summary.Overview))
            throw new ArgumentException("Analysis summary overview is required.", nameof(command.Result));
    }
}
