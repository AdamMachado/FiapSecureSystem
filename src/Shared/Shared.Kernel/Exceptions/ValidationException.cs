namespace Shared.Kernel.Exceptions;

public class ValidationException : AppException
{
    public ValidationException(string message)
        : base(message)
    {
        Errors = Array.Empty<string>();
    }

    public ValidationException(IEnumerable<string> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors?.ToArray() ?? Array.Empty<string>();
    }

    public IReadOnlyCollection<string> Errors { get; }

    public override string Code => "validation_error";
}