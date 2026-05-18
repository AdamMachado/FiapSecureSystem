using Shared.Kernel.Primitives;

namespace IdentityService.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!normalized.Contains('@', StringComparison.Ordinal) || normalized.StartsWith('@') || normalized.EndsWith('@'))
            throw new ArgumentException("Email address is invalid.", nameof(value));

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
