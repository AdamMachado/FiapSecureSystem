using Microsoft.Extensions.DependencyInjection;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Integration.Consumed;
using ProcessingService.Application.Integration.Published;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Application.UseCases.ExecuteAnalysisProcessing;
using ProcessingService.Application.UseCases.FailAnalysisProcessing;
using ProcessingService.Application.UseCases.GetProcessingResult;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using ProcessingService.Domain.Events;
using Shared.Contracts.IntegrationEvents;

namespace ProcessingService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProcessingApplication(this IServiceCollection services)
    {
        services.AddScoped<StartAnalysisProcessingHandler>();
        services.AddScoped<ExecuteAnalysisProcessingHandler>();
        services.AddScoped<GetProcessingResultHandler>();
        services.AddScoped<FailAnalysisProcessingHandler>();
        services.AddScoped<CompleteAnalysisProcessingHandler>();

        services.AddScoped<
            IIntegrationEventHandler<AnalysisRequestedIntegrationEvent>,
            AnalysisRequestedMessageHandler>();

        services.AddScoped<
            IIntegrationEventHandler<AnalysisExecutionRequestedIntegrationEvent>,
            AnalysisExecutionRequestedMessageHandler>();

        services.AddScoped<
            IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent>,
            AnalysisStartedIntegrationEventMapper>();

        services.AddScoped<
            IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent>,
            AnalysisCompletedIntegrationEventMapper>();

        services.AddScoped<
            IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent>,
            AnalysisFailedIntegrationEventMapper>();

        services.AddScoped<AnalysisExecutionRequestedIntegrationEventFactory>();

        return services;
    }
}
