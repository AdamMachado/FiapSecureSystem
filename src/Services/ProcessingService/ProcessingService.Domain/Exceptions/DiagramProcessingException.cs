using Shared.Kernel.Exceptions;

namespace ProcessingService.Domain.Exceptions;

public sealed class DiagramProcessingException : DomainException
{
    public DiagramProcessingException(string message)
        : base(message)
    {
    }

    public DiagramProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
