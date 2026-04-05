namespace Shared.Kernel.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message)
        : base(message)
    {
    }

    protected AppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public virtual string Code => "app_error";
}