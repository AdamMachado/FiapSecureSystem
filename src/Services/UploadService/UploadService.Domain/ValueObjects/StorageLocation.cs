using Shared.Kernel.Primitives;

namespace UploadService.Domain.ValueObjects;

public sealed class StorageLocation : ValueObject
{
    public string BucketName { get; }
    public string ObjectKey { get; }

    private StorageLocation(string bucketName, string objectKey)
    {
        BucketName = bucketName;
        ObjectKey = objectKey;
    }

    public static StorageLocation Create(string bucketName, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name cannot be empty.", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key cannot be empty.", nameof(objectKey));

        return new StorageLocation(bucketName.Trim(), objectKey.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BucketName;
        yield return ObjectKey;
    }
}