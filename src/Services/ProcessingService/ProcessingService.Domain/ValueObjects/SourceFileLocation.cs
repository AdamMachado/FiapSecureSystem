using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;

namespace ProcessingService.Domain.ValueObjects;

public sealed class SourceFileLocation : ValueObject
{
    public string BucketName { get; }
    public string ObjectKey { get; }

    private SourceFileLocation(string bucketName, string objectKey)
    {
        BucketName = bucketName;
        ObjectKey = objectKey;
    }

    public static SourceFileLocation Create(string bucketName, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ValidationException("Source file bucket name is required.");

        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ValidationException("Source file object key is required.");

        return new SourceFileLocation(bucketName.Trim(), objectKey.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BucketName;
        yield return ObjectKey;
    }
}
