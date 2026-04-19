using Shared.Kernel.Exceptions;

namespace ReportService.Infrastructure.Exceptions;

public sealed class ReportRenderingException : AppException
{
    public ReportRenderingException(string message, Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException(message))
    {
    }

    public override string Code => "report_rendering_error";
}