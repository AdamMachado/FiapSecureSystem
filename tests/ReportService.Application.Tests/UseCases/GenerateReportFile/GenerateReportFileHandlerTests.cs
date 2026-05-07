using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.Mappings;
using ReportService.Application.Tests.TestData;
using ReportService.Application.UseCases.GenerateReportFile;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Events;
using Shared.Contracts.IntegrationEvents.Abstractions;
using System.Diagnostics;
using Xunit;

namespace ReportService.Application.Tests.UseCases.GenerateReportFile;

public sealed class GenerateReportFileHandlerTests
{
    private readonly Mock<IAnalysisReportRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IReportRenderer> _renderer = new();
    private readonly Mock<IReportStorage> _storage = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<IIntegrationEventMapper<ReportGeneratedDomainEvent>> _mapper = new();
    private readonly ActivitySource _activitySource = new("ReportService.Application.Tests");

    private GenerateReportFileHandler CreateHandler()
    {
        return new GenerateReportFileHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            new[] { _renderer.Object },
            _storage.Object,
            _publisher.Object,
            _mapper.Object,
            _activitySource);
    }

    [Fact]
    public async Task HandleAsync_Should_Generate_File_When_It_Does_Not_Exist()
    {
        var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        var report = AnalysisReport.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnalysisReportMappings.ToAnalysisJson(AnalysisResultFactory.Create()),
            now);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(report.AnalysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _renderer
            .Setup(x => x.CanRender(ReportFormat.Pdf))
            .Returns(true);

        _renderer
            .Setup(x => x.RenderAsync(It.IsAny<RenderReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RenderedReport("analysis-report.pdf", "application/pdf", new byte[] { 1, 2, 3 }));

        _storage
            .Setup(x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredReportDescriptor(
                "analysis-reports",
                "reports/analysis-report.pdf",
                "analysis-report.pdf",
                "application/pdf"));

        _mapper
            .Setup(x => x.Map(It.IsAny<ReportGeneratedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GenerateReportFileCommand(report.AnalysisRequestId, ReportFormat.Pdf),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        report.Files.Should().ContainSingle(x => x.Format == ReportFormat.Pdf);

        _repository.Verify(x => x.Update(report), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Existing_File_Without_Rendering_When_Format_Already_Exists()
    {
        var report = AnalysisReport.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnalysisReportMappings.ToAnalysisJson(AnalysisResultFactory.Create()),
            DateTime.UtcNow);

        report.AddFile(
            Guid.NewGuid(),
            ReportFormat.Markdown,
            "analysis-reports",
            "reports/analysis-report.md",
            "analysis-report.md",
            "text/markdown",
            DateTime.UtcNow);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(report.AnalysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GenerateReportFileCommand(report.AnalysisRequestId, ReportFormat.Markdown),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Format.Should().Be(ReportFormat.Markdown);

        _storage.Verify(x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}
