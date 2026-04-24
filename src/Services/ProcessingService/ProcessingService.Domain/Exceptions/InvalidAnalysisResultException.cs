using Shared.Kernel.Exceptions;

namespace ProcessingService.Domain.Exceptions;

public sealed class InvalidAnalysisResultException : DomainException
{
    public InvalidAnalysisResultException(string reason)
        : base($"The generated analysis result is invalid. {reason}")
    {
        Reason = reason;
    }

    public string Reason { get; }
}
