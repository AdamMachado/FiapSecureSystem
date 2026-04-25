using Microsoft.Extensions.DependencyInjection;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.Integration.Consumed;
using Shared.Contracts.IntegrationEvents;
using UploadService.Application.UseCases.CreateAnalysis;
using UploadService.Application.UseCases.GetAnalysisStatus;
using UploadService.Application.UseCases.UpdateAnalysisStatus;

namespace UploadService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddUploadApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateAnalysisValidator>();

        services.AddScoped<CreateAnalysisHandler>();
        services.AddScoped<GetAnalysisStatusHandler>();
        services.AddScoped<UpdateAnalysisStatusHandler>();

        services.AddScoped<IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>, AnalysisCompletedMessageHandler>();
        services.AddScoped<IIntegrationEventHandler<AnalysisFailedIntegrationEvent>, AnalysisFailedMessageHandler>();
        services.AddScoped<IIntegrationEventHandler<AnalysisStartedIntegrationEvent>, AnalysisStartedMessageHandler>();

        return services;
    }
}