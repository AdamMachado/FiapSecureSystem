namespace UploadService.Application.UseCases.CreateAnalysis;

public sealed record CreateAnalysisCommand(
    string FileName,
    string ContentType,
    long SizeInBytes,
    Stream Content,
    string FileHash);