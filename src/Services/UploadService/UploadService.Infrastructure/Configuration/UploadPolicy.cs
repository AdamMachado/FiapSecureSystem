using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using UploadService.Domain.Enums;
using UploadService.Infrastructure.Configuration.Options;

namespace UploadService.Infrastructure.Configuration;

public sealed class UploadPolicy : IUploadPolicy
{
    private readonly UploadOptions _options;

    public UploadPolicy(IOptions<UploadOptions> options)
    {
        _options = options.Value;
    }

    public long MaxFileSizeInBytes => _options.MaxFileSizeInBytes;

    public bool IsContentTypeSupported(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return _options.SupportedContentTypes.Contains(
            contentType.Trim(),
            StringComparer.OrdinalIgnoreCase);
    }

    public FileType ResolveFileType(string contentType) => contentType.Trim().ToLowerInvariant() switch
    {
        "application/pdf" => FileType.Pdf,
        "image/png" => FileType.Png,
        "image/jpeg" => FileType.Jpeg,
        "image/jpg" => FileType.Jpeg,
        _ => throw new ValidationException($"Unsupported content type '{contentType}'.")
    };
}