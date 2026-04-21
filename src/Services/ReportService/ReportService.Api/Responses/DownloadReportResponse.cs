namespace ReportService.Api.Contracts.Responses;

public sealed record DownloadReportResponse(
    string FileName,
    string ContentType,
    byte[] Content);