using Shared.Kernel.Exceptions;

namespace UploadService.Domain.Exceptions;

public sealed class UnsupportedFileTypeException : DomainException
{
    public string ProvidedContentType { get; }

    public UnsupportedFileTypeException(string providedContentType)
        : base($"The uploaded file type '{providedContentType}' is not supported.")
    {
        ProvidedContentType = providedContentType;
    }
}