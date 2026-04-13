using Shared.Kernel.Primitives;
using UploadService.Domain.Enums;

namespace UploadService.Domain.ValueObjects;

public sealed class FileMetadata : ValueObject
{
    public string FileName { get; }
    public string ContentType { get; }
    public long SizeInBytes { get; }
    public FileType FileType { get; }

    private FileMetadata(
        string fileName,
        string contentType,
        long sizeInBytes,
        FileType fileType)
    {
        FileName = fileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        FileType = fileType;
    }

    public static FileMetadata Create(
        string fileName,
        string contentType,
        long sizeInBytes,
        FileType fileType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));

        if (sizeInBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "File size must be greater than zero.");

        return new FileMetadata(
            fileName.Trim(),
            contentType.Trim().ToLowerInvariant(),
            sizeInBytes,
            fileType);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return ContentType;
        yield return SizeInBytes;
        yield return FileType;
    }
}