using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Abstractions.Storage;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Application.UseCases.FailAnalysisProcessing;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;
using System.Diagnostics;

namespace ProcessingService.Application.Tests.UseCases.StartAnalysisProcessing;

public sealed class StartAnalysisProcessingHandlerTests
{
    private readonly Mock<IAnalysisProcessRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IArchitectureAnalyzer> _analyzer = new();

    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent>> _startedMapper = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent>> _completedMapper = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent>> _failedMapper = new();

    private readonly Mock<ILogger<CompleteAnalysisProcessingHandler>> _loggerComplete = new();
    private readonly Mock<ILogger<FailAnalysisProcessingHandler>> _loggerFail = new();
    private readonly Mock<ILogger<StartAnalysisProcessingHandler>> _loggerStart = new();

    private readonly ActivitySource _activitySource = new("ProcessingService.Application.Tests");

    private StartAnalysisProcessingHandler CreateHandler()
    {
        var completeHandler = new CompleteAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _eventPublisher.Object,
            _completedMapper.Object,
            _loggerComplete.Object,
            _activitySource);

        var failHandler = new FailAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _eventPublisher.Object,
            _failedMapper.Object,
            _loggerFail.Object,
            _activitySource);

        return new StartAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _objectStorage.Object,
            new[] { _analyzer.Object },
            _eventPublisher.Object,
            _startedMapper.Object,
            completeHandler,
            failHandler,
            _loggerStart.Object,
            _activitySource);
    }

    [Fact]
    public async Task HandleAsync_Should_Start_Analyze_And_Complete_Process_When_Command_Is_Valid()
    {
        var now = DateTime.UtcNow;
        var analysisRequestId = Guid.NewGuid();
        var requestedByUserId = Guid.NewGuid();

        _dateTimeProvider
            .Setup(x => x.UtcNow)
            .Returns(now);

        _repository
            .Setup(x => x.ExistsByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRequestId id, CancellationToken _) =>
                CreateStartedProcess(id.Value, requestedByUserId, now));

        _startedMapper
            .Setup(x => x.Map(It.IsAny<AnalysisProcessingStartedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        _completedMapper
            .Setup(x => x.Map(It.IsAny<AnalysisProcessingCompletedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        _objectStorage
            .Setup(x => x.DownloadAsync(
                It.IsAny<DownloadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredObjectContent(
                Content: new MemoryStream(new byte[] { 1, 2, 3 }),
                ContentType: "application/pdf",
                SizeInBytes: 3,
                ETag: "etag-test"));

        _analyzer
            .Setup(x => x.CanHandle(DiagramType.Pdf))
            .Returns(true);

        var component = new IdentifiedComponentDto(
            Id: "comp-1",
            Name: "API",
            Type: ComponentType.Backend,
            Description: "Backend API",
            Tags: Array.Empty<string>(),
            ConnectedTo: Array.Empty<string>(),
            Metadata: null);

        _analyzer
            .Setup(x => x.AnalyzeAsync(
                It.IsAny<ArchitectureAnalysisRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArchitectureAnalysisResult(
                ExtractedText: "texto extraido",
                Components: new[] { component },
                Risks: Array.Empty<ArchitecturalRiskDto>(),
                Recommendations: Array.Empty<ArchitecturalRecommendationDto>(),
                Overview: "Processamento concluído",
                RequiresManualReview: false,
                Warnings: Array.Empty<string>()));

        var handler = CreateHandler();

        var command = new StartAnalysisProcessingCommand(
            AnalysisRequestId: analysisRequestId,
            RequestedByUserId: requestedByUserId,
            FileName: "diagram.pdf",
            ContentType: "application/pdf",
            FileHash: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            StorageBucket: "analysis-uploads",
            StorageObjectKey: "uploads/diagram.pdf");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        result.Value.AnalysisRequestId.Should().Be(analysisRequestId);
        result.Value.Status.Should().Be(ProcessingStatus.Completed);

        _repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisProcess>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _objectStorage.Verify(
            x => x.DownloadAsync(It.IsAny<DownloadObjectRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _analyzer.Verify(
            x => x.AnalyzeAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Process_Already_Exists()
    {
        var analysisRequestId = Guid.NewGuid();

        _repository
            .Setup(x => x.ExistsByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var command = new StartAnalysisProcessingCommand(
            AnalysisRequestId: analysisRequestId,
            RequestedByUserId: Guid.NewGuid(),
            FileName: "diagram.pdf",
            ContentType: "application/pdf",
            FileHash: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            StorageBucket: "analysis-uploads",
            StorageObjectKey: "uploads/diagram.pdf");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisProcess>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _objectStorage.Verify(
            x => x.DownloadAsync(It.IsAny<DownloadObjectRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AnalysisProcess CreateStartedProcess(
        Guid analysisRequestId,
        Guid requestedByUserId,
        DateTime now)
    {
        var process = AnalysisProcess.Create(
            id: Guid.NewGuid(),
            analysisRequestId: AnalysisRequestId.Create(analysisRequestId),
            requestedByUserId: requestedByUserId,
            sourceFileLocation: SourceFileLocation.Create("analysis-uploads", "uploads/diagram.pdf"),
            diagramType: DiagramType.Pdf,
            createdAtUtc: now);

        process.MarkAsStarted(now);

        return process;
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}