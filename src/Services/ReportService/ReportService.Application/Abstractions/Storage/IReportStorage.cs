namespace ReportService.Application.Abstractions.Storage;

public interface IReportStorage
{
    Task<StoredReportDescriptor> UploadAsync(
        UploadReportRequest request,
        CancellationToken cancellationToken = default);

    Task<DownloadedReportDescriptor?> DownloadAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);
}

public sealed record UploadReportRequest(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record StoredReportDescriptor(
    string BucketName,
    string ObjectKey,
    string FileName,
    string ContentType);

public sealed record DownloadedReportDescriptor(
    string FileName,
    string ContentType,
    Stream Content);