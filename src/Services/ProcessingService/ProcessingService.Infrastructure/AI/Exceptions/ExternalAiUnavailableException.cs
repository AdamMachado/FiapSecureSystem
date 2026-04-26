using Shared.Kernel.Exceptions;

namespace ProcessingService.Infrastructure.AI.Exceptions;

public sealed class ExternalAiUnavailableException : AppException
{
    public ExternalAiUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException(message))
    {
    }

    public override string Code => "external_ai_unavailable";
}
