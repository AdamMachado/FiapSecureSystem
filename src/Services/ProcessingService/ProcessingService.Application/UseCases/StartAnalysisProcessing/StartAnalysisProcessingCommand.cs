namespace ProcessingService.Application.UseCases.StartAnalysisProcessing;

public sealed record StartAnalysisProcessingCommand(
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    string FileName,
    string ContentType,
    string FileHash,
    string StorageBucket,
    string StorageObjectKey);
