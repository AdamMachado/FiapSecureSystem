using Shared.Kernel.Exceptions;

namespace ProcessingService.Infrastructure.Exceptions;

public sealed class StorageUnavailableException : AppException
{
    public StorageUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException(message))
    {
    }

    public override string Code => "storage_unavailable";
}
