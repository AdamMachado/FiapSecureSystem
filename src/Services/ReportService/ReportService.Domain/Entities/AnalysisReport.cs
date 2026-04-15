using FiapSecureSystem.ReportService.Domain.Enums;
using FiapSecureSystem.ReportService.Domain.Events;
using FiapSecureSystem.ReportService.Domain.Exceptions;
using FiapSecureSystem.ReportService.Domain.ValueObjects;
using Shared.Kernel.Primitives;

namespace FiapSecureSystem.ReportService.Domain.Entities;

public sealed class AnalysisReport : AggregateRoot<ReportId>
{
    public AnalysisRequestId AnalysisRequestId { get; private set; } = null!;
    public ReportStatus Status { get; private set; }
    public ReportFormat Format { get; private set; }
    public ReportContent? Content { get; private set; }
    public GeneratedFileLocation? FileLocation { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? GeneratedAtUtc { get; private set; }

    private AnalysisReport()
    {
    }

    private AnalysisReport(
        ReportId id,
        AnalysisRequestId analysisRequestId,
        ReportFormat format,
        DateTime createdAtUtc) : base(id)
    {
        AnalysisRequestId = analysisRequestId;
        Format = format;
        Status = ReportStatus.Pending;
        CreatedAtUtc = createdAtUtc;

        RaiseDomainEvent(new ReportGenerationRequestedDomainEvent(Id, AnalysisRequestId));
    }

    public static AnalysisReport Create(
        AnalysisRequestId analysisRequestId,
        ReportFormat format,
        DateTime createdAtUtc)
    {
        ValidateFormat(format);

        return new AnalysisReport(
            ReportId.New(),
            analysisRequestId,
            format,
            createdAtUtc);
    }

    public void MarkAsGenerating(DateTime updatedAtUtc)
    {
        if (Status == ReportStatus.Generated)
            throw new ReportGenerationException("A generated report cannot return to generating state.");

        Status = ReportStatus.Generating;
        FailureReason = null;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Complete(
        ReportContent content,
        GeneratedFileLocation fileLocation,
        DateTime generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(fileLocation);

        Content = content;
        FileLocation = fileLocation;
        Status = ReportStatus.Generated;
        FailureReason = null;
        UpdatedAtUtc = generatedAtUtc;
        GeneratedAtUtc = generatedAtUtc;

        RaiseDomainEvent(new ReportGeneratedDomainEvent(Id, AnalysisRequestId));
    }

    public void Fail(string reason, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ReportGenerationException("Failure reason must be informed.");

        Status = ReportStatus.Failed;
        FailureReason = reason.Trim();
        UpdatedAtUtc = updatedAtUtc;

        RaiseDomainEvent(new ReportGenerationFailedDomainEvent(Id, AnalysisRequestId, FailureReason));
    }

    private static void ValidateFormat(ReportFormat format)
    {
        if (!Enum.IsDefined(typeof(ReportFormat), format))
            throw new UnsupportedReportFormatException(format.ToString());
    }
}