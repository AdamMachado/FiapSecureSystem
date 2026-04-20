using Shared.Kernel.Primitives;

namespace ReportService.Domain.ValueObjects;

public sealed class AnalysisRequestId : ValueObject
{
    public Guid Value { get; }

    private AnalysisRequestId(Guid value)
    {
        Value = value;
    }

    public static AnalysisRequestId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AnalysisRequestId cannot be empty.", nameof(value));

        return new AnalysisRequestId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}