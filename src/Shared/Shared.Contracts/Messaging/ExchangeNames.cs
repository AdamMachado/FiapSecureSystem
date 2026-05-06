namespace Shared.Contracts.Messaging;

public static class ExchangeNames
{
    public const string Analysis = "analysis.exchange";
    public const string AnalysisDeadLetter = "analysis.dlx";

    public const string Report = "report.exchange";
    public const string ReportDeadLetter = "report.dlx";
}