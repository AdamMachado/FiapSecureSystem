namespace ReportService.Infrastructure.Configuration.Options;

public sealed class ReportOptions
{
    public const string SectionName = "Report";

    public string DefaultFormat { get; init; } = "json";
    public string FileNamePrefix { get; init; } = "report";
}