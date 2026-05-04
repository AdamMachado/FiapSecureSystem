using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;

namespace ProcessingService.Domain.ValueObjects;

public sealed class ExtractedText : ValueObject
{
    public string Value { get; }

    private ExtractedText(string value)
    {
        Value = value;
    }

    public static ExtractedText Create(string? value)
    {
        return new ExtractedText(value?.Trim() ?? string.Empty);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
