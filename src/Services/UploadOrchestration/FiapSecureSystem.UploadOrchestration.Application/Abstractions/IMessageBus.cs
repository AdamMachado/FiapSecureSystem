namespace FiapSecureSystem.UploadOrchestration.Application.Abstractions;

public interface IMessageBus
{
    Task PublishAnalysisRequestedAsync(object message, CancellationToken cancellationToken);
}