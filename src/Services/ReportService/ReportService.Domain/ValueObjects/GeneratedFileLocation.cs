using Shared.Kernel.Primitives;

namespace FiapSecureSystem.ReportService.Domain.ValueObjects;

public sealed class GeneratedFileLocation : ValueObject
{
    public string Bucket { get; }
    public string ObjectKey { get; }

    private GeneratedFileLocation(string bucket, string objectKey)
    {
        Bucket = bucket;
        ObjectKey = objectKey;
    }

    public static GeneratedFileLocation Create(string bucket, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            throw new ArgumentException("Bucket cannot be empty.", nameof(bucket));

        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("ObjectKey cannot be empty.", nameof(objectKey));

        return new GeneratedFileLocation(bucket.Trim(), objectKey.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Bucket;
        yield return ObjectKey;
    }

    public override string ToString() => $"{Bucket}/{ObjectKey}";
}