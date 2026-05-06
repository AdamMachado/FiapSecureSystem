using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.Integration.Consumed;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Domain.Entities;
using ReportService.Domain.Events;
using ReportService.Domain.Enums;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Schemas;
using Xunit;
using System.Diagnostics;

namespace ReportService.Application.Tests.Integration.Consumed;

public sealed class AnalysisCompletedMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Generate_Report_When_Event_Is_Received()
    {
        var repository = new Mock<IAnalysisReportRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var renderer = new Mock<IReportRenderer>();
        var storage = new Mock<IReportStorage>();
        var publisher = new Mock<IEventPublisher>();
        var mapper = new Mock<IIntegrationEventMapper<ReportGeneratedDomainEvent>>();
        var activitySource = new ActivitySource("ReportService.Application.Tests");

    var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        renderer
            .Setup(x => x.RenderAsync(It.IsAny<RenderReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RenderedReport(
                "analysis-report.pdf",
                "application/pdf",
                new byte[] { 1, 2, 3 }));

        storage
            .Setup(x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredReportDescriptor(
                "analysis-reports",
                "reports/analysis-report.pdf",
                "analysis-report.pdf",
                "application/pdf"));

        mapper
            .Setup(x => x.Map(It.IsAny<ReportGeneratedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        var generateReportHandler = new GenerateReportHandler(
            new GenerateReportValidator(),
            repository.Object,
            unitOfWork.Object,
            dateTimeProvider.Object,
            renderer.Object,
            storage.Object,
            publisher.Object,
            mapper.Object,
            activitySource);

        var handler = new AnalysisCompletedMessageHandler(generateReportHandler);

        var analysisRequestId = Guid.NewGuid();
        var requestedByUserId = Guid.NewGuid();

        var integrationEvent = new AnalysisCompletedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            analysisRequestId,
            requestedByUserId,
            now,
            new AnalysisResultDto(
                Array.Empty<IdentifiedComponentDto>(),
                Array.Empty<ArchitecturalRiskDto>(),
                Array.Empty<ArchitecturalRecommendationDto>(),
                new AnalysisSummaryDto(
                    "Teste",
                    0,
                    0,
                    0,
                    false,
                    Array.Empty<string>())));

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        storage.Verify(
            x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        publisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}