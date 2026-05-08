using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.Mappings;
using ReportService.Application.Tests.TestData;
using ReportService.Application.UseCases.DownloadReportFile;
using ReportService.Application.UseCases.GenerateReportFile;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Events;
using Shared.Contracts.IntegrationEvents.Abstractions;
using System.Diagnostics;
using Xunit;

namespace ReportService.Application.Tests.UseCases.DownloadReportFile;

public sealed class DownloadReportFileHandlerTests
{
    private readonly Mock<IAnalysisReportRepository> _repository = new();
    private readonly Mock<IReportStorage> _storage = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IReportRenderer> _renderer = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<IIntegrationEventMapper<ReportGeneratedDomainEvent>> _mapper = new();
    private readonly ActivitySource _activitySource = new("ReportService.Application.Tests");

    private DownloadReportFileHandler CreateHandler()
    {
        var generateReportFileHandler = new GenerateReportFileHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            new[] { _renderer.Object },
            _storage.Object,
            _publisher.Object,
            _mapper.Object,
            _activitySource);

        return new DownloadReportFileHandler(
            _repository.Object,
            _storage.Object,
            generateReportFileHandler,
            _activitySource);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Existing_File_When_Report_File_Already_Exists()
    {
        var report = AnalysisReport.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnalysisReportMappings.ToAnalysisJson(AnalysisResultFactory.Create()),
            DateTime.UtcNow);

        report.AddFile(
            Guid.NewGuid(),
            ReportFormat.Pdf,
            "analysis-reports",
            "reports/report.pdf",
            "report.pdf",
            "application/pdf",
            DateTime.UtcNow);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(report.AnalysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _storage
            .Setup(x => x.DownloadAsync("analysis-reports", "reports/report.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadedReportDescriptor(
                "report.pdf",
                "application/pdf",
                new MemoryStream(new byte[] { 1, 2, 3 })));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new DownloadReportFileQuery(report.AnalysisRequestId, ReportFormat.Pdf),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("report.pdf");

        _storage.Verify(
            x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Generate_Then_Download_File_When_It_Does_Not_Exist()
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

        _renderer.Setup(x => x.CanRender(ReportFormat.Pdf)).Returns(true);
        _renderer
            .Setup(x => x.RenderAsync(It.IsAny<RenderReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RenderedReport("analysis-report.pdf", "application/pdf", new byte[] { 1, 2, 3 }));

        _storage
            .Setup(x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredReportDescriptor(
                "analysis-reports",
                "reports/generated.pdf",
                "analysis-report.pdf",
                "application/pdf"));

        _storage
            .Setup(x => x.DownloadAsync("analysis-reports", "reports/generated.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadedReportDescriptor(
                "analysis-report.pdf",
                "application/pdf",
                new MemoryStream(new byte[] { 9, 9, 9 })));

        _mapper
            .Setup(x => x.Map(It.IsAny<ReportGeneratedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new DownloadReportFileQuery(report.AnalysisRequestId, ReportFormat.Pdf),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        report.Files.Should().ContainSingle(x => x.Format == ReportFormat.Pdf);

        _renderer.Verify(
            x => x.RenderAsync(
                It.Is<RenderReportRequest>(request =>
                    request.Format == ReportFormat.Pdf &&
                    request.FileNameWithoutExtension == $"analysis-report-{report.AnalysisRequestId:N}" &&
                    request.AnalysisResult.Summary.Overview == "Teste"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _storage.Verify(x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(x => x.DownloadAsync("analysis-reports", "reports/generated.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Report_Does_Not_Exist()
    {
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new DownloadReportFileQuery(Guid.NewGuid(), ReportFormat.Markdown),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _storage.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}
