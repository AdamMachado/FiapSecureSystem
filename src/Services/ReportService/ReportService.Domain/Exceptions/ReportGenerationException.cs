using Shared.Kernel.Exceptions;

namespace ReportService.Domain.Exceptions;

public sealed class ReportGenerationException : DomainException
{
    public ReportGenerationException(string message)
        : base(message)
    {
    }
}