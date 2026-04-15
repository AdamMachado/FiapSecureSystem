using Shared.Kernel.Primitives;

namespace FiapSecureSystem.ReportService.Domain.ValueObjects;

public sealed class ReportId : ValueObject
{
    public Guid Value { get; }

    private ReportId(Guid value)
    {
        Value = value;
    }

    public static ReportId New() => new(Guid.NewGuid());

    public static ReportId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ReportId cannot be empty.", nameof(value));

        return new ReportId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}