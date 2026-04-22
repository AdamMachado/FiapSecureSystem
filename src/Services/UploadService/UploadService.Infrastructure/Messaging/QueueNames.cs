namespace UploadService.Infrastructure.Messaging;

public static class QueueNames
{
    public const string AnalysisRequested = "analysis.requested";
    public const string AnalysisStarted = "analysis.started";
    public const string AnalysisCompleted = "analysis.completed";
    public const string AnalysisFailed = "analysis.failed";
}
