using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Abstractions.Storage;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Application.UseCases.ExecuteAnalysisProcessing;
using ProcessingService.Application.UseCases.FailAnalysisProcessing;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;
using System.Diagnostics;

namespace ProcessingService.Application.Tests.UseCases.ExecuteAnalysisProcessing;

public sealed class ExecuteAnalysisProcessingHandlerTests
{
    private readonly Mock<IAnalysisProcessRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IArchitectureAnalyzer> _analyzer = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent>> _completedMapper = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent>> _failedMapper = new();
    private readonly Mock<ILogger<CompleteAnalysisProcessingHandler>> _completeLogger = new();
    private readonly Mock<ILogger<FailAnalysisProcessingHandler>> _failLogger = new();
    private readonly Mock<ILogger<ExecuteAnalysisProcessingHandler>> _executeLogger = new();
    private readonly ActivitySource _activitySource = new("ProcessingService.Application.Tests");

    private ExecuteAnalysisProcessingHandler CreateHandler()
    {
        var completeHandler = new CompleteAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _eventPublisher.Object,
            _completedMapper.Object,
            _completeLogger.Object,
            _activitySource);

        var failHandler = new FailAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _eventPublisher.Object,
            _failedMapper.Object,
            _failLogger.Object,
            _activitySource);

        return new ExecuteAnalysisProcessingHandler(
            _repository.Object,
            _objectStorage.Object,
            new[] { _analyzer.Object },
            completeHandler,
            failHandler,
            _executeLogger.Object,
            _activitySource);
    }

    [Fact]
    public async Task HandleAsync_Should_Download_Analyze_And_Complete_When_Process_Is_Processing()
    {
        var analysisRequestId = Guid.NewGuid();
        var requestedByUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var process = CreateStartedProcess(analysisRequestId, requestedByUserId, now);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now.AddMinutes(1));
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _completedMapper
            .Setup(x => x.Map(It.IsAny<AnalysisProcessingCompletedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        _objectStorage
            .Setup(x => x.DownloadAsync(It.IsAny<DownloadObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredObjectContent(
                new MemoryStream(new byte[] { 1, 2, 3 }),
                "application/pdf",
                3,
                "etag"));

        _analyzer.Setup(x => x.CanHandle(DiagramType.Pdf)).Returns(true);
        _analyzer
            .Setup(x => x.AnalyzeAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchitectureAnalysisResult(
                new[]
                {
                    new IdentifiedComponentDto(
                        "comp-1",
                        "API",
                        ComponentType.Backend,
                        "Backend API",
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        null)
                },
                Array.Empty<ArchitecturalRiskDto>(),
                Array.Empty<ArchitecturalRecommendationDto>(),
                "texto",
                "overview",
                false,
                Array.Empty<string>()));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExecuteAnalysisProcessingCommand(
                process.Id,
                analysisRequestId,
                requestedByUserId,
                "diagram.pdf",
                "application/pdf",
                "hash",
                "analysis-uploads",
                "uploads/diagram.pdf"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ProcessingStatus.Completed);
        process.Status.Should().Be(ProcessingStatus.Completed);

        _objectStorage.Verify(x => x.DownloadAsync(It.IsAny<DownloadObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _analyzer.Verify(x => x.AnalyzeAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.Update(It.Is<AnalysisProcess>(p => p == process)), Times.Once);
        _eventPublisher.Verify(x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_Process_When_No_Analyzer_Can_Handle_DiagramType()
    {
        var analysisRequestId = Guid.NewGuid();
        var requestedByUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var process = CreateStartedProcess(analysisRequestId, requestedByUserId, now);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now.AddMinutes(1));
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _failedMapper
            .Setup(x => x.Map(It.IsAny<AnalysisProcessingFailedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        _objectStorage
            .Setup(x => x.DownloadAsync(It.IsAny<DownloadObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredObjectContent(
                new MemoryStream(new byte[] { 1, 2, 3 }),
                "application/pdf",
                3,
                "etag"));

        _analyzer.Setup(x => x.CanHandle(DiagramType.Pdf)).Returns(false);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExecuteAnalysisProcessingCommand(
                process.Id,
                analysisRequestId,
                requestedByUserId,
                "diagram.pdf",
                "application/pdf",
                "hash",
                "analysis-uploads",
                "uploads/diagram.pdf"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ProcessingStatus.Failed);
        process.Status.Should().Be(ProcessingStatus.Failed);
        process.FailureReason.Should().Contain("No architecture analyzer found");

        _analyzer.Verify(x => x.AnalyzeAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(x => x.Update(It.Is<AnalysisProcess>(p => p == process)), Times.Once);
        _eventPublisher.Verify(x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Conflict_When_Process_Is_Not_In_Processing_Status()
    {
        var process = AnalysisProcess.Create(
            Guid.NewGuid(),
            AnalysisRequestId.Create(Guid.NewGuid()),
            Guid.NewGuid(),
            SourceFileLocation.Create("analysis-uploads", "uploads/diagram.pdf"),
            DiagramType.Pdf,
            DateTime.UtcNow);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == process.AnalysisRequestId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExecuteAnalysisProcessingCommand(
                process.Id,
                process.AnalysisRequestId.Value,
                process.RequestedByUserId,
                "diagram.pdf",
                "application/pdf",
                "hash",
                "analysis-uploads",
                "uploads/diagram.pdf"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("processing.invalid_status");

        _objectStorage.Verify(x => x.DownloadAsync(It.IsAny<DownloadObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AnalysisProcess CreateStartedProcess(Guid analysisRequestId, Guid requestedByUserId, DateTime startedAtUtc)
    {
        var process = AnalysisProcess.Create(
            Guid.NewGuid(),
            AnalysisRequestId.Create(analysisRequestId),
            requestedByUserId,
            SourceFileLocation.Create("analysis-uploads", "uploads/diagram.pdf"),
            DiagramType.Pdf,
            startedAtUtc);

        process.MarkAsStarted(startedAtUtc);
        process.DequeueDomainEvents();

        return process;
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}
