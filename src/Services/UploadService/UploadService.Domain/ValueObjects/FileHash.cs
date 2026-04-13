using Shared.Kernel.Primitives;

namespace UploadService.Domain.ValueObjects;

public sealed class FileHash : ValueObject
{
    public string Value { get; }

    private FileHash(string value)
    {
        Value = value;
    }

    public static FileHash Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("File hash cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        // Assumindo SHA-256 em hexadecimal: 64 chars
        if (normalized.Length != 64 || normalized.Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("File hash must be a valid SHA-256 hexadecimal string.", nameof(value));

        return new FileHash(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}