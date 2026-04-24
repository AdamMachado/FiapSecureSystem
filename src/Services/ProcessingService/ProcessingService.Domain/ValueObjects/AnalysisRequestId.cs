using Shared.Kernel.Exceptions;
using Shared.Kernel.Primitives;

namespace ProcessingService.Domain.ValueObjects;

public sealed class AnalysisRequestId : ValueObject
{
    public Guid Value { get; }

    private AnalysisRequestId(Guid value)
    {
        Value = value;
    }

    public static AnalysisRequestId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ValidationException("Analysis request id must be a non-empty GUID.");

        return new AnalysisRequestId(value);
    }

    public override string ToString() => Value.ToString();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
