using System.Diagnostics;

namespace Shared.Observability.Telemetry;

public static class ActivitySources
{
    public const string ApiGateway = "SOAT.ApiGateway";
    public const string WebApp = "SOAT.WebApp";
    public const string UploadService = "SOAT.UploadService";
    public const string ProcessingService = "SOAT.ProcessingService";
    public const string ReportService = "SOAT.ReportService";

    public static ActivitySource Create(string sourceName) => new(sourceName);
}