using UploadService.Domain.Enums;

namespace UploadService.Application.UseCases.CreateAnalysis;

public sealed class CreateAnalysisValidator
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg"
    };

    public void ValidateAndThrow(CreateAnalysisCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.FileName))
            throw new ArgumentException("File name is required.", nameof(command.FileName));

        if (string.IsNullOrWhiteSpace(command.ContentType))
            throw new ArgumentException("Content type is required.", nameof(command.ContentType));

        if (command.SizeInBytes <= 0)
            throw new ArgumentException("File size must be greater than zero.", nameof(command.SizeInBytes));

        if (command.Content is null)
            throw new ArgumentException("Content stream is required.", nameof(command.Content));

        if (string.IsNullOrWhiteSpace(command.FileHash))
            throw new ArgumentException("File hash is required.", nameof(command.FileHash));

        if (!SupportedContentTypes.Contains(command.ContentType))
            throw new ArgumentException($"Unsupported content type '{command.ContentType}'.", nameof(command.ContentType));
    }

    public static FileType ResolveFileType(string contentType)
        => contentType.Trim().ToLowerInvariant() switch
        {
            "application/pdf" => FileType.Pdf,
            "image/png" => FileType.Png,
            "image/jpeg" => FileType.Jpeg,
            "image/jpg" => FileType.Jpeg,
            _ => throw new ArgumentOutOfRangeException(nameof(contentType), $"Unsupported content type '{contentType}'.")
        };
}