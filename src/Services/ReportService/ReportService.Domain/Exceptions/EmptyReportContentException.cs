using Shared.Kernel.Exceptions;

namespace FiapSecureSystem.ReportService.Domain.Exceptions;

public sealed class EmptyReportContentException : DomainException
{
    public EmptyReportContentException()
        : base("Report content cannot be empty.")
    {
    }
}