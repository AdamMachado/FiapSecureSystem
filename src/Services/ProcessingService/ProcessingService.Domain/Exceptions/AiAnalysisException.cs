using Shared.Kernel.Exceptions;

namespace ProcessingService.Domain.Exceptions;

public sealed class AiAnalysisException : DomainException
{
    public AiAnalysisException(string operation, string message)
        : base($"AI analysis failed during '{operation}'. {message}")
    {
        Operation = operation;
    }

    public AiAnalysisException(string operation, string message, Exception innerException)
        : base($"AI analysis failed during '{operation}'. {message}", innerException)
    {
        Operation = operation;
    }

    public string Operation { get; }
}
