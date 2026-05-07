namespace UploadService.Application.UseCases.GetAnalysisAsset;

public sealed record GetAnalysisAssetResult(
    Stream Content,
    string ContentType,
    string FileName,
    long? SizeInBytes);
