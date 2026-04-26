using Microsoft.Extensions.DependencyInjection;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Integration.Consumed;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Application.UseCases.FailAnalysisProcessing;
using ProcessingService.Application.UseCases.GetProcessingResult;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using Shared.Contracts.IntegrationEvents;

namespace ProcessingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProcessingApplication(this IServiceCollection services)
    {
        services.AddScoped<StartAnalysisProcessingHandler>();
        services.AddScoped<GetProcessingResultHandler>();
        services.AddScoped<FailAnalysisProcessingHandler>();
        services.AddScoped<CompleteAnalysisProcessingHandler>();

        services.AddScoped<IIntegrationEventHandler<AnalysisRequestedIntegrationEvent>, AnalysisRequestedMessageHandler>();

        return services;
    }
}
