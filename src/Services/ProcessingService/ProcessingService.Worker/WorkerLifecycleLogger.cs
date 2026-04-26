namespace ProcessingService.Worker;

internal sealed class WorkerLifecycleLogger : IHostedService
{
    private readonly ILogger<WorkerLifecycleLogger> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public WorkerLifecycleLogger(
        ILogger<WorkerLifecycleLogger> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ProcessingService.Worker started. Environment={Environment}, RabbitMqHost={RabbitMqHost}, OpenAiModel={OpenAiModel}",
            _environment.EnvironmentName,
            _configuration["RabbitMq:Host"],
            _configuration["OpenAI:Model"]);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProcessingService.Worker stopping.");
        return Task.CompletedTask;
    }
}
