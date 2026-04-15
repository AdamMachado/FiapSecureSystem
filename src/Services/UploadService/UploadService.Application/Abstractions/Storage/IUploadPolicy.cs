using UploadService.Domain.Enums;

public interface IUploadPolicy
{
    long MaxFileSizeInBytes { get; }
    bool IsContentTypeSupported(string contentType);
    FileType ResolveFileType(string contentType);
}