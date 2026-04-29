using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Application.UseCases.FailAnalysisProcessing;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Xunit;

namespace ProcessingService.Application.Tests.UseCases.FailAnalysisProcessing;

public sealed class CompleteAnalysisProcessingHandlerTests
{
    private readonly Mock<IAnalysisProcessRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingFailedDomainEvent>> _mapper = new();
    private readonly Mock<ILogger<FailAnalysisProcessingHandler>> _logger = new();

    private FailAnalysisProcessingHandler CreateHandler()
    {
        return new FailAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _eventPublisher.Object,
            _mapper.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_Should_Mark_Process_As_Failed()
    {
        var analysisRequestId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        var process = CreateProcess(analysisRequestId);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _mapper
            .Setup(x => x.Map(It.IsAny<AnalysisProcessingFailedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        var handler = CreateHandler();

        var command = new FailAnalysisProcessingCommand(
            analysisRequestId,
            "AI error",
            "details"
        );

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        process.Status.Should().Be(ProcessingStatus.Failed);
        process.FailureReason.Should().Be("AI error");
        process.FailureDetails.Should().Be("details");
        process.FailedAtUtc.Should().Be(now);

        _repository.Verify(x => x.Update(process), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Process_Not_Found()
    {
        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.IsAny<AnalysisRequestId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisProcess?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new FailAnalysisProcessingCommand(Guid.NewGuid(), "error", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _repository.Verify(x => x.Update(It.IsAny<AnalysisProcess>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AnalysisProcess CreateProcess(Guid analysisRequestId)
    {
        var process = AnalysisProcess.Create(
            id: Guid.NewGuid(),
            analysisRequestId: AnalysisRequestId.Create(analysisRequestId),
            requestedByUserId: Guid.NewGuid(),
            sourceFileLocation: SourceFileLocation.Create("bucket", "file.pdf"),
            diagramType: DiagramType.Pdf,
            createdAtUtc: DateTime.UtcNow);

        process.MarkAsStarted(DateTime.UtcNow);

        return process;
    }
    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null) { }
    }
}