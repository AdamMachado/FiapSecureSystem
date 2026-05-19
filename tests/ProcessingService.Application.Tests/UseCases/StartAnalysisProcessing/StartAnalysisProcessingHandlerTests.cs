using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.Integration.Published;
using ProcessingService.Application.UseCases.StartAnalysisProcessing;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;
using Shared.Observability.Correlation;
using System.Diagnostics;

namespace ProcessingService.Application.Tests.UseCases.StartAnalysisProcessing;

public sealed class StartAnalysisProcessingHandlerTests
{
    private readonly Mock<IAnalysisProcessRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingStartedDomainEvent>> _startedMapper = new();
    private readonly Mock<ICorrelationContextAccessor> _correlationContextAccessor = new();
    private readonly Mock<ILogger<StartAnalysisProcessingHandler>> _logger = new();
    private readonly ActivitySource _activitySource = new("ProcessingService.Application.Tests");

    private readonly Guid _correlationId = Guid.NewGuid();
    private readonly Guid _causationId = Guid.NewGuid();

    private StartAnalysisProcessingHandler CreateHandler()
    {
        _correlationContextAccessor.Setup(x => x.GetOrCreateCorrelationGuid()).Returns(_correlationId);
        _correlationContextAccessor.Setup(x => x.GetCausationGuidOrNull()).Returns(_causationId);

        return new StartAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _eventPublisher.Object,
            _startedMapper.Object,
            new AnalysisExecutionRequestedIntegrationEventFactory(_correlationContextAccessor.Object),
            _logger.Object,
            _activitySource);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Process_And_Publish_Started_And_Execution_Events_When_Process_Does_Not_Exist()
    {
        var now = DateTime.UtcNow;
        var analysisRequestId = Guid.NewGuid();
        var requestedByUserId = Guid.NewGuid();
        var startedEvent = new FakeIntegrationEvent();

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisProcess?)null);

        _startedMapper
            .Setup(x => x.Map(It.IsAny<AnalysisProcessingStartedDomainEvent>()))
            .Returns(startedEvent);

        var handler = CreateHandler();

        var command = new StartAnalysisProcessingCommand(
            analysisRequestId,
            requestedByUserId,
            "diagram.pdf",
            "application/pdf",
            "hash",
            "analysis-uploads",
            "uploads/diagram.pdf");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AnalysisRequestId.Should().Be(analysisRequestId);
        result.Value.Status.Should().Be(ProcessingStatus.Processing);
        result.Value.StartedAtUtc.Should().Be(now);
        result.Value.CompletedAtUtc.Should().BeNull();
        result.Value.FailedAtUtc.Should().BeNull();

        _repository.Verify(
            x => x.AddAsync(
                It.Is<AnalysisProcess>(p =>
                    p.AnalysisRequestId.Value == analysisRequestId &&
                    p.RequestedByUserId == requestedByUserId &&
                    p.DiagramType == DiagramType.Pdf &&
                    p.Status == ProcessingStatus.Processing),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(
            x => x.PublishAsync(
                It.Is<IntegrationEventBase>(e => ReferenceEquals(e, startedEvent)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _eventPublisher.Verify(
            x => x.PublishAsync(
                It.Is<AnalysisExecutionRequestedIntegrationEvent>(e =>
                    e.AnalysisRequestId == analysisRequestId &&
                    e.RequestedByUserId == requestedByUserId &&
                    e.FileName == "diagram.pdf" &&
                    e.ContentType == "application/pdf" &&
                    e.FileHash == "hash" &&
                    e.StorageBucket == "analysis-uploads" &&
                    e.StorageObjectKey == "uploads/diagram.pdf" &&
                    e.CorrelationId == _correlationId &&
                    e.CausationId == _causationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Requeue_Execution_For_Existing_Processing_Process_Without_Creating_New_One()
    {
        var now = DateTime.UtcNow;
        var process = CreateStartedProcess(Guid.NewGuid(), Guid.NewGuid(), now);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now.AddMinutes(1));
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == process.AnalysisRequestId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        var handler = CreateHandler();

        var command = new StartAnalysisProcessingCommand(
            process.AnalysisRequestId.Value,
            process.RequestedByUserId,
            "diagram.pdf",
            "application/pdf",
            "hash",
            "analysis-uploads",
            "uploads/diagram.pdf");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AnalysisProcessId.Should().Be(process.Id);
        result.Value.Status.Should().Be(ProcessingStatus.Processing);

        _repository.Verify(x => x.AddAsync(It.IsAny<AnalysisProcess>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(x => x.Update(It.IsAny<AnalysisProcess>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<AnalysisExecutionRequestedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _eventPublisher.Verify(
            x => x.PublishAsync(It.Is<FakeIntegrationEvent>(_ => true), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Terminal_Process_Without_Publishing_Anything_When_Process_Is_Completed()
    {
        var now = DateTime.UtcNow;
        var process = CreateCompletedProcess(Guid.NewGuid(), Guid.NewGuid(), now);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now.AddMinutes(1));
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == process.AnalysisRequestId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        var handler = CreateHandler();

        var command = new StartAnalysisProcessingCommand(
            process.AnalysisRequestId.Value,
            process.RequestedByUserId,
            "diagram.pdf",
            "application/pdf",
            "hash",
            "analysis-uploads",
            "uploads/diagram.pdf");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ProcessingStatus.Completed);
        result.Value.CompletedAtUtc.Should().Be(process.CompletedAtUtc);

        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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

    private static AnalysisProcess CreateCompletedProcess(Guid analysisRequestId, Guid requestedByUserId, DateTime timestamp)
    {
        var process = CreateStartedProcess(analysisRequestId, requestedByUserId, timestamp);

        process.MarkAsCompleted(
            ExtractedText.Create("text"),
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
            ProcessingResultSummary.Create("overview", 1, 0, 0, false, Array.Empty<string>()),
            timestamp.AddMinutes(1));

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
