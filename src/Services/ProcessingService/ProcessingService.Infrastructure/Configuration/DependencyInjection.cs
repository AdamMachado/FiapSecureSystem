using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Abstractions.Storage;
using ProcessingService.Infrastructure.AI.Diagnostics;
using ProcessingService.Infrastructure.AI.Guardrails;
using ProcessingService.Infrastructure.AI.Inspection;
using ProcessingService.Infrastructure.AI.OpenAI;
using ProcessingService.Infrastructure.AI.Options;
using ProcessingService.Infrastructure.AI.Policies;
using ProcessingService.Infrastructure.AI.Validation;
using ProcessingService.Infrastructure.Clock;
using ProcessingService.Infrastructure.Configuration.Options;
using ProcessingService.Infrastructure.HealthChecks;
using ProcessingService.Infrastructure.Messaging.RabbitMq;
using ProcessingService.Infrastructure.Messaging.RabbitMq.Internals;
using ProcessingService.Infrastructure.Persistence.Context;
using ProcessingService.Infrastructure.Persistence.Repositories;
using ProcessingService.Infrastructure.Persistence.UnitOfWork;
using ProcessingService.Infrastructure.Storage.MinIO;
using RabbitMQ.Client;
using Shared.Observability.Correlation;

namespace ProcessingService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddProcessingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<MinIoOptions>(configuration.GetSection(MinIoOptions.SectionName));
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<ArchitectureAnalysisOptions>(configuration.GetSection(ArchitectureAnalysisOptions.SectionName));

        services.AddCorrelationContext();

        services.AddDbContext<ProcessingDbContext>((sp, options) =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseNpgsql(
                databaseOptions.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

            if (databaseOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IAnalysisProcessRepository, AnalysisProcessRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinIoOptions>>().Value;

            return new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl)
                .Build();
        });

        services.AddScoped<IObjectStorage, MinIoObjectStorage>();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            return new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.Username,
                Password = options.Password,
                ClientProvidedName = options.ClientProvidedName,
                Ssl = { Enabled = options.UseSsl }
            };
        });

        services.AddSingleton<RabbitMqChannel>(sp =>
        {
            var factory = sp.GetRequiredService<ConnectionFactory>();
            return RabbitMqChannel.CreateAsync(factory).GetAwaiter().GetResult();
        });

        services.AddSingleton<RabbitMqMessageDispatcher>();
        services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
        services.AddHostedService<RabbitMqSubscriberService>();

        services.AddHttpClient<OpenAiArchitectureAnalysisClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://api.openai.com/v1/"
                : options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScoped<ArchitectureAnalysisInputValidator>();
        services.AddScoped<FileSignatureValidator>();
        services.AddScoped<AiInputCostPolicy>();
        services.AddScoped<AiServiceTierPolicy>();
        services.AddScoped<ArchitectureAnalysisPromptBuilder>();
        services.AddScoped<ArchitectureAnalysisSanitizer>();
        services.AddScoped<ArchitectureAnalysisOutputValidator>();
        services.AddScoped<ArchitectureAnalysisResponseMapper>();
        services.AddScoped<AiUsageLogger>();
        services.AddScoped<IAnalysisFileInspector, ImageAnalysisFileInspector>();
        services.AddScoped<IAnalysisFileInspector, PdfAnalysisFileInspector>();
        services.AddScoped<IArchitectureAnalyzer, OpenAiArchitectureAnalyzer>();

        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        services.AddHealthChecks()
            .AddPostgreSqlHealthChecks()
            .AddCheck<RabbitMqHealthCheck>(
                name: "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["messaging", "rabbitmq"])
            .AddCheck<MinIoHealthCheck>(
                name: "minio",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["storage", "minio"])
            .AddCheck<OpenAiHealthCheck>(
                name: "openai",
                failureStatus: HealthStatus.Degraded,
                tags: ["ai", "openai"]);

        return services;
    }
}
