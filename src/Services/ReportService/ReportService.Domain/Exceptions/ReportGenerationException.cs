using Shared.Kernel.Exceptions;

namespace FiapSecureSystem.ReportService.Domain.Exceptions;

public sealed class ReportGenerationException : DomainException
{
    public ReportGenerationException(string message)
        : base(message)
    {
    }
}