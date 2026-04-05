namespace Shared.Kernel.Exceptions;

public class DomainException : AppException
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public override string Code => "domain_error";
}