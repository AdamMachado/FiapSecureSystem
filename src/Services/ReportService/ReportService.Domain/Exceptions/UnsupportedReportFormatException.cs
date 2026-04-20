using Shared.Kernel.Exceptions;

namespace ReportService.Domain.Exceptions;

public sealed class UnsupportedReportFormatException : DomainException
{
    public UnsupportedReportFormatException(string format)
        : base($"Report format '{format}' is not supported.")
    {
    }
}