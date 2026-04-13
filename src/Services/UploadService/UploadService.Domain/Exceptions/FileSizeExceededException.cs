using Shared.Kernel.Exceptions;

namespace UploadService.Domain.Exceptions;

public sealed class FileSizeExceededException : DomainException
{
    public long MaxAllowedSizeInBytes { get; }
    public long ProvidedSizeInBytes { get; }

    public FileSizeExceededException(long maxAllowedSizeInBytes, long providedSizeInBytes)
        : base($"The uploaded file exceeds the allowed size limit. Max: {maxAllowedSizeInBytes} bytes, Provided: {providedSizeInBytes} bytes.")
    {
        MaxAllowedSizeInBytes = maxAllowedSizeInBytes;
        ProvidedSizeInBytes = providedSizeInBytes;
    }
}