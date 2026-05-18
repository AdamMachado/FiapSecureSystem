namespace ProcessingService.Application.UseCases.ExecuteAnalysisProcessing;

public sealed record ExecuteAnalysisProcessingCommand(
    Guid AnalysisProcessId,
    Guid AnalysisRequestId,
    Guid RequestedByUserId,
    string FileName,
    string ContentType,
    string FileHash,
    string StorageBucket,
    string StorageObjectKey);
