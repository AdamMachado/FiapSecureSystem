using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Integration.Consumed;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Application.Tests.TestData;
using ReportService.Domain.Entities;
using Shared.Contracts.IntegrationEvents;
using System.Diagnostics;
using Xunit;

namespace ReportService.Application.Tests.Integration.Consumed;

public sealed class AnalysisCompletedMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Generate_Base_Report_When_Event_Is_Received()
    {
        var repository = new Mock<IAnalysisReportRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var activitySource = new ActivitySource("ReportService.Application.Tests");

        var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        var generateReportHandler = new GenerateReportHandler(
            new GenerateReportValidator(),
            repository.Object,
            unitOfWork.Object,
            dateTimeProvider.Object,
            activitySource);

        var handler = new AnalysisCompletedMessageHandler(generateReportHandler);

        var integrationEvent = new AnalysisCompletedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now,
            AnalysisResultFactory.Create());

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Create_Duplicate_Base_Report_When_It_Already_Exists()
    {
        var repository = new Mock<IAnalysisReportRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        var activitySource = new ActivitySource("ReportService.Application.Tests");

        var existingReport = AnalysisReport.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "{\"summary\":{\"overview\":\"Teste\"}}",
            DateTime.UtcNow);

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(existingReport.AnalysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReport);

        var generateReportHandler = new GenerateReportHandler(
            new GenerateReportValidator(),
            repository.Object,
            unitOfWork.Object,
            dateTimeProvider.Object,
            activitySource);

        var handler = new AnalysisCompletedMessageHandler(generateReportHandler);

        var integrationEvent = new AnalysisCompletedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            existingReport.AnalysisRequestId,
            existingReport.RequestedByUserId,
            DateTime.UtcNow,
            AnalysisResultFactory.Create());

        var action = async () => await handler.HandleAsync(integrationEvent, CancellationToken.None);

        await action.Should().NotThrowAsync();
        repository.Verify(x => x.AddAsync(It.IsAny<AnalysisReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
