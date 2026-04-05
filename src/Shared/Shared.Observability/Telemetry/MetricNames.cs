namespace Shared.Observability.Telemetry;

public static class MetricNames
{
    public const string UploadRequestsTotal = "upload_requests_total";
    public const string AnalysisRequestsTotal = "analysis_requests_total";
    public const string AnalysisCompletedTotal = "analysis_completed_total";
    public const string AnalysisFailedTotal = "analysis_failed_total";
    public const string ReportGeneratedTotal = "report_generated_total";

    public const string AnalysisDurationMs = "analysis_duration_ms";
    public const string ReportGenerationDurationMs = "report_generation_duration_ms";
}