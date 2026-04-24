using Shared.Kernel.Exceptions;

namespace ProcessingService.Domain.Exceptions;

public sealed class UnsupportedDiagramFormatException : DomainException
{
    public UnsupportedDiagramFormatException(string providedContentType)
        : base($"The diagram format '{providedContentType}' is not supported for processing.")
    {
        ProvidedContentType = providedContentType;
    }

    public string ProvidedContentType { get; }
}
