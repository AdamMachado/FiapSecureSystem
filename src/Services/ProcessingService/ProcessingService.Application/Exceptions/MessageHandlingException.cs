using Shared.Kernel.Exceptions;

namespace ProcessingService.Application.Exceptions;

public sealed class MessageHandlingException : AppException
{
    public MessageHandlingException(
        string message,
        string? errorCode = null,
        Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException(message))
    {
        ErrorCode = errorCode;
    }

    public string? ErrorCode { get; }

    public override string Code => "message_handling_error";
}