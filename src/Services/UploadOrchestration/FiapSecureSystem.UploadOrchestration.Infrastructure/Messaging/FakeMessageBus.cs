using FiapSecureSystem.UploadOrchestration.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FiapSecureSystem.UploadOrchestration.Infrastructure.Messaging;

public class FakeMessageBus : IMessageBus
{
    private readonly ILogger<FakeMessageBus> _logger;

    public FakeMessageBus(ILogger<FakeMessageBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAnalysisRequestedAsync(object message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fake publish AnalysisRequested: {@Message}", message);
        return Task.CompletedTask;
    }
}