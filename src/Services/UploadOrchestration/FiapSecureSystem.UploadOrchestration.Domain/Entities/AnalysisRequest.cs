using FiapSecureSystem.UploadOrchestration.Domain.Enums;

namespace FiapSecureSystem.UploadOrchestration.Domain.Entities;

public class AnalysisRequest
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string StoragePath { get; private set; } = default!;
    public AnalysisStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AnalysisRequest() { }

    public AnalysisRequest(string fileName, string contentType, string storagePath, string correlationId)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        ContentType = contentType;
        StoragePath = storagePath;
        CorrelationId = correlationId;
        Status = AnalysisStatus.Received;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = AnalysisStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsAnalyzed()
    {
        Status = AnalysisStatus.Analyzed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsError(string errorMessage)
    {
        Status = AnalysisStatus.Error;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}