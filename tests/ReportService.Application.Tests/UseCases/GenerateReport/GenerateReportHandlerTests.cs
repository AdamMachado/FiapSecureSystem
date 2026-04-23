using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Messaging;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Domain.Entities;
using ReportService.Domain.Events;
using ReportService.Domain.Enums;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Schemas;
using Xunit;

namespace ReportService.Application.Tests.UseCases.GenerateReport;

public sealed class GenerateReportHandlerTests
{
    private readonly Mock<IAnalysisReportRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IReportStorage> _storage = new();
    private readonly Mock<IReportRenderer> _renderer = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IIntegrationEventMapper<ReportGeneratedDomainEvent>> _generatedEventMapper = new();

    private GenerateReportHandler CreateHandler()
    {
        return new GenerateReportHandler(
            new GenerateReportValidator(),
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _renderer.Object,
            _storage.Object,
            _eventPublisher.Object,
            _generatedEventMapper.Object);
    }

    [Fact]
    public async Task HandleAsync_Should_Generate_Report_When_Command_Is_Valid()
    {
        var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        _renderer
            .Setup(x => x.RenderAsync(It.IsAny<RenderReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RenderedReport(
                "analysis-report.pdf",
                "application/pdf",
                new byte[] { 1, 2, 3 }));

        _storage
            .Setup(x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredReportDescriptor(
                "analysis-reports",
                "reports/analysis-report.pdf",
                "analysis-report.pdf",
                "application/pdf"));

        _generatedEventMapper
            .Setup(x => x.Map(It.IsAny<ReportGeneratedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        var handler = CreateHandler();

        var command = new GenerateReportCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
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
                    Array.Empty<string>())),
            ReportFormat.Pdf);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _storage.Verify(
            x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Report_Already_Exists()
    {
        var existingReport = AnalysisReport.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportFormat.Pdf,
            "conteudo",
            "analysis-reports",
            "reports/existing.pdf",
            "existing.pdf",
            "application/pdf",
            DateTime.UtcNow);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReport);

        var handler = CreateHandler();

        var command = new GenerateReportCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
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
                    Array.Empty<string>())),
            ReportFormat.Pdf);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _storage.Verify(
            x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Renderer_Throws_Validation_Exception()
    {
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        _renderer
            .Setup(x => x.RenderAsync(It.IsAny<RenderReportRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Shared.Kernel.Exceptions.ValidationException("invalid render request"));

        var handler = CreateHandler();

        var command = new GenerateReportCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
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
                    Array.Empty<string>())),
            ReportFormat.Pdf);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _storage.Verify(
            x => x.UploadAsync(It.IsAny<UploadReportRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}