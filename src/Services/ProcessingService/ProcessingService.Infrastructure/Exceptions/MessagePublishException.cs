using Shared.Kernel.Exceptions;

namespace ProcessingService.Infrastructure.Exceptions;

public sealed class MessagePublishException : AppException
{
    public MessagePublishException(string message, Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException(message))
    {
    }

    public override string Code => "message_publish_error";
}
