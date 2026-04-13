using Shared.Kernel.Primitives;

namespace UploadService.Domain.ValueObjects;

public sealed class StorageObjectKey : ValueObject
{
    public string Value { get; }

    private StorageObjectKey(string value)
    {
        Value = value;
    }

    public static StorageObjectKey Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Storage object key cannot be empty.", nameof(value));

        var normalized = value.Trim();

        if (normalized.Length > 512)
            throw new ArgumentException("Storage object key cannot exceed 512 characters.", nameof(value));

        return new StorageObjectKey(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}