namespace UploadService.Application.Abstractions.Storage;

public interface IStorageSettings
{
    string BucketName { get; }
}