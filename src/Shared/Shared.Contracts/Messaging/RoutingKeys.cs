namespace Shared.Contracts.Messaging;

public static class RoutingKeys
{
    public const string AnalysisRequested = "analysis.requested";
    public const string AnalysisStarted = "analysis.started";
    public const string AnalysisCompleted = "analysis.completed";
    public const string AnalysisFailed = "analysis.failed";
    public const string ReportGenerated = "report.generated";
}