using Microsoft.Extensions.DependencyInjection;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Integration.Consumed;
using ReportService.Application.UseCases.DownloadReport;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Application.UseCases.GetReportByAnalysis;
using ReportService.Application.UseCases.UpdateReportStatus;
using Shared.Contracts.IntegrationEvents;

namespace ReportService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReportApplication(this IServiceCollection services)
    {
        services.AddScoped<GenerateReportValidator>();

        services.AddScoped<GenerateReportHandler>();
        services.AddScoped<GetReportByAnalysisHandler>();
        services.AddScoped<DownloadReportHandler>();
        services.AddScoped<UpdateReportStatusHandler>();

        services.AddScoped<IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>, AnalysisCompletedMessageHandler>();

        return services;
    }
}