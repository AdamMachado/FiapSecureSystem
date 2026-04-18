using UploadService.Application.Abstractions.Storage;

namespace UploadService.Infrastructure.Storage;

public sealed class StorageObjectKeyFactory : IStorageObjectKeyFactory
{
    public string CreateForAnalysisUpload(Guid analysisRequestId, string originalFileName, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);

        var extension = Path.GetExtension(originalFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(
            fileNameWithoutExtension
                .Trim()
                .Select(c => invalidChars.Contains(c) ? '_' : c)
                .ToArray());

        sanitized = sanitized.Replace(' ', '-');

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "upload";

        return $"uploads/{nowUtc:yyyy/MM/dd}/{analysisRequestId:N}-{sanitized}{extension}";
    }
}
