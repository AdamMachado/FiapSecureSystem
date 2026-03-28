namespace FiapSecureSystem.UploadOrchestration.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, CancellationToken cancellationToken);
}