using Shared.Kernel.Exceptions;

namespace FiapSecureSystem.ReportService.Domain.Exceptions;

public sealed class UnsupportedReportFormatException : DomainException
{
    public UnsupportedReportFormatException(string format)
        : base($"Report format '{format}' is not supported.")
    {
    }
}