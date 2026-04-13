using Shared.Kernel.Exceptions;

namespace UploadService.Domain.Exceptions;

public sealed class EmptyUploadException : DomainException
{
    public EmptyUploadException()
        : base("The uploaded file is empty.")
    {
    }
}