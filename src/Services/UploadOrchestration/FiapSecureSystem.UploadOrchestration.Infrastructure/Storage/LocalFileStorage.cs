using FiapSecureSystem.UploadOrchestration.Application.Abstractions;

namespace FiapSecureSystem.UploadOrchestration.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(string basePath)
    {
        _basePath = basePath;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_basePath);

        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_basePath, safeFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return fullPath;
    }
}

