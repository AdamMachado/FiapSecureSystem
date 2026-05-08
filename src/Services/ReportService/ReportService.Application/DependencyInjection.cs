using Microsoft.Extensions.DependencyInjection;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Integration.Consumed;
using ReportService.Application.UseCases.DownloadReportFile;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Application.UseCases.GenerateReportFile;
using ReportService.Application.UseCases.GetReportByAnalysis;
using Shared.Contracts.IntegrationEvents;

namespace ReportService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReportApplication(this IServiceCollection services)
    {
        services.AddScoped<GenerateReportValidator>();

        services.AddScoped<GenerateReportHandler>();
        services.AddScoped<GenerateReportFileHandler>();
        services.AddScoped<GetReportByAnalysisHandler>();
        services.AddScoped<DownloadReportFileHandler>();

        services.AddScoped<IIntegrationEventHandler<AnalysisCompletedIntegrationEvent>, AnalysisCompletedMessageHandler>();

        return services;
    }
}
