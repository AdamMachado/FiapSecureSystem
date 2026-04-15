using UploadService.Application.Abstractions.Storage;

namespace UploadService.Infrastructure.Storage;

public sealed class StorageObjectKeyFactory : IStorageObjectKeyFactory
{
    public string CreateForAnalysisUpload(
        Guid analysisRequestId,
        string fileName,
        DateTime nowUtc)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(
            fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

        sanitized = sanitized.Replace(' ', '-');

        return $"uploads/{nowUtc:yyyy/MM/dd}/{analysisRequestId:N}-{sanitized}";
    }
}