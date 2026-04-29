using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using RabbitMQ.Client;
using Shared.Contracts.IntegrationEvents;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.Abstractions.Storage;
using UploadService.Application.Integration.Consumed;
using UploadService.Application.Integration.Published;
using UploadService.Domain.Events;
using UploadService.Infrastructure.Configuration.Options;
using UploadService.Infrastructure.HealthChecks;
using UploadService.Infrastructure.Identity;
using UploadService.Infrastructure.Messaging.RabbitMq;
using UploadService.Infrastructure.Messaging.RabbitMq.Internals;
using UploadService.Infrastructure.Persistence.Context;
using UploadService.Infrastructure.Persistence.Repositories;
using UploadService.Infrastructure.Persistence.UnitOfWork;
using UploadService.Infrastructure.Storage;
using UploadService.Infrastructure.Storage.MinIO;

namespace UploadService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddUploadInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<UploadOptions>(configuration.GetSection(UploadOptions.SectionName));
        services.Configure<MinIoOptions>(configuration.GetSection(MinIoOptions.SectionName));

        services.AddCorrelationContext();

        services.AddDbContext<UploadDbContext>((sp, options) =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseNpgsql(
                databaseOptions.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

            if (databaseOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IAnalysisRequestRepository, AnalysisRequestRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton<IStorageObjectKeyFactory, StorageObjectKeyFactory>();
        services.AddSingleton<IUploadPolicy, UploadPolicy>();
        services.AddSingleton<IUserContext, StubUserContext>();

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
                Ssl =
                {
                    Enabled = options.UseSsl,
                    ServerName = options.Host
                }
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

        services.AddScoped<IIntegrationEventMapper<AnalysisRequestCreatedDomainEvent>, AnalysisRequestedIntegrationEventMapper>();
        services.AddScoped<IIntegrationEventHandler<AnalysisStartedIntegrationEvent>, AnalysisStartedMessageHandler>();
        services.AddScoped<IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>, AnalysisCompletedMessageHandler>();
        services.AddScoped<IIntegrationEventHandler<AnalysisFailedIntegrationEvent>, AnalysisFailedMessageHandler>();

        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        services.AddSharedHealthChecks();

        services.AddHealthChecks()
            .AddPostgreSqlHealthChecks(database)
            .AddCheck<RabbitMqHealthCheck>("rabbitmq")
            .AddCheck<MinIoHealthCheck>("minio");

        return services;
    }
}
