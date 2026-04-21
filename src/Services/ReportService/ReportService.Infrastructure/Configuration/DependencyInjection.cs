using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using RabbitMQ.Client;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.Integration.Consumed;
using ReportService.Application.Integration.Published;
using ReportService.Domain.Events;
using ReportService.Infrastructure.Configuration.Options;
using ReportService.Infrastructure.HealthChecks;
using ReportService.Infrastructure.Messaging.RabbitMq;
using ReportService.Infrastructure.Messaging.RabbitMq.Internals;
using ReportService.Infrastructure.Persistence.Context;
using ReportService.Infrastructure.Persistence.Repositories;
using ReportService.Infrastructure.Persistence.UnitOfWork;
using ReportService.Infrastructure.Rendering.Json;
using ReportService.Infrastructure.Rendering.Markdown;
using ReportService.Infrastructure.Rendering.Pdf;
using ReportService.Infrastructure.Storage.MinIO;
using Shared.Contracts.IntegrationEvents;
using Shared.Observability.Correlation;
using Shared.Observability.HealthChecks;
using System.Runtime.Serialization;
using ReportService.Application.Abstractions.Clock;
using ReportService.Infrastructure.Clock;

namespace ReportService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddReportInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<ReportOptions>(configuration.GetSection(ReportOptions.SectionName));
        services.Configure<MinIoOptions>(configuration.GetSection(MinIoOptions.SectionName));

        services.AddCorrelationContext();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddDbContext<ReportDbContext>((sp, options) =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseNpgsql(
                databaseOptions.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema));

            if (databaseOptions.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IAnalysisReportRepository, AnalysisReportRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinIoOptions>>().Value;

            return new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl)
                .Build();
        });

        services.AddScoped<IReportStorage, MinIoReportStorage>();

        services.AddScoped<IReportRenderer, JsonReportRenderer>();
        services.AddScoped<IReportRenderer, MarkdownReportRenderer>();
        services.AddScoped<IReportRenderer, PdfReportRenderer>();

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
                ClientProvidedName = options.ClientProvidedName
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

        services.AddScoped<IIntegrationEventMapper<ReportGeneratedDomainEvent>, ReportGeneratedIntegrationEventMapper>();
        services.AddScoped<IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>, AnalysisCompletedMessageHandler>();

        var database = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        services.AddSharedHealthChecks();

        services.AddHealthChecks()
            .AddPostgreSqlHealthChecks(database)
            .AddCheck<RabbitMqHealthCheck>("rabbitmq")
            .AddCheck<MinIoHealthCheck>("minio");

        return services;
    }
}