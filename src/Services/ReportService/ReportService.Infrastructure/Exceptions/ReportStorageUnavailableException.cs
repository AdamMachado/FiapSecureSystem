using Shared.Kernel.Exceptions;

namespace ReportService.Infrastructure.Exceptions;

public sealed class ReportStorageUnavailableException : AppException
{
    public ReportStorageUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException(message))
    {
    }

    public override string Code => "report_storage_unavailable";
}