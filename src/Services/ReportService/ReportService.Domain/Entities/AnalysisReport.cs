using System.Text.Json;
using ReportService.Domain.Enums;
using ReportService.Domain.Events;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;

namespace ReportService.Domain.Entities;

public sealed class AnalysisReport : AggregateRoot<Guid>
{
    private readonly List<AnalysisReportFile> _files = [];

    private AnalysisReport()
    {
    }

    private AnalysisReport(
        Guid id,
        Guid analysisRequestId,
        Guid requestedByUserId,
        string analysisData,
        DateTime createdAtUtc)
        : base(id)
    {
        if (analysisRequestId == Guid.Empty)
            throw new ArgumentException("AnalysisRequestId cannot be empty.", nameof(analysisRequestId));

        if (requestedByUserId == Guid.Empty)
            throw new ArgumentException("RequestedByUserId cannot be empty.", nameof(requestedByUserId));

        if (string.IsNullOrWhiteSpace(analysisData))
            throw new ArgumentException("Analysis data cannot be empty.", nameof(analysisData));

        EnsureValidJson(analysisData);

        AnalysisRequestId = analysisRequestId;
        RequestedByUserId = requestedByUserId;
        AnalysisData = analysisData.Trim();
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid AnalysisRequestId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string AnalysisData { get; private set; } = default!;
    public IReadOnlyCollection<AnalysisReportFile> Files => _files.AsReadOnly();
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static AnalysisReport Create(
        Guid id,
        Guid analysisRequestId,
        Guid requestedByUserId,
        string analysisData,
        DateTime createdAtUtc)
    {
        return new AnalysisReport(
            id,
            analysisRequestId,
            requestedByUserId,
            analysisData,
            createdAtUtc);
    }

    public AnalysisReportFile? GetFile(ReportFormat format)
        => _files.FirstOrDefault(x => x.Format == format);

    public AnalysisReportFile AddFile(
        Guid fileId,
        ReportFormat format,
        string bucketName,
        string objectKey,
        string fileName,
        string contentType,
        DateTime createdAtUtc)
    {
        if (GetFile(format) is not null)
            throw new DomainException($"A report file for format '{format}' already exists.");

        var file = AnalysisReportFile.Create(
            fileId,
            Id,
            format,
            bucketName,
            objectKey,
            contentType,
            fileName,
            createdAtUtc);

        _files.Add(file);
        UpdatedAtUtc = createdAtUtc;

        RaiseDomainEvent(new ReportGeneratedDomainEvent(
            Id,
            AnalysisRequestId,
            RequestedByUserId,
            file.BucketName,
            file.ObjectKey,
            file.FileName,
            file.CreatedAtUtc));

        return file;
    }

    private static void EnsureValidJson(string analysisData)
    {
        using var _ = JsonDocument.Parse(analysisData);
    }
}
