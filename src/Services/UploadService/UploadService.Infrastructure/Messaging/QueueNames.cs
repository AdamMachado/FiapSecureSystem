namespace UploadService.Infrastructure.Messaging;

public static class QueueNames
{
    public const string AnalysisStarted = "upload.analysis.started";
    public const string AnalysisCompleted = "upload.analysis.completed";
    public const string AnalysisFailed = "upload.analysis.failed";

    public const string ReportGenerated = "upload.report.generated";
}
