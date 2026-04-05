namespace Shared.Observability.Telemetry;

public static class TelemetryConstants
{
    public const string ServiceNameConfigKey = "OpenTelemetry:ServiceName";
    public const string ServiceVersionConfigKey = "OpenTelemetry:ServiceVersion";
    public const string OtlpEndpointConfigKey = "OpenTelemetry:Otlp:Endpoint";

    public const string DefaultServiceVersion = "1.0.0";
}