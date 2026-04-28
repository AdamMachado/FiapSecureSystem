using FluentAssertions;
using Moq;
using ProcessingService.Application.Abstractions.Clock;
using ProcessingService.Application.Abstractions.Messaging;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.UseCases.CompleteAnalysisProcessing;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Events;
using ProcessingService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Contracts.IntegrationEvents.Enums;
using Shared.Contracts.IntegrationEvents.Schemas;

namespace ProcessingService.Application.Tests.UseCases.CompleteAnalysisProcessing;

public sealed class CompleteAnalysisProcessingHandlerTests
{
    private readonly Mock<IAnalysisProcessRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisProcessingCompletedDomainEvent>> _mapper = new();

    private CompleteAnalysisProcessingHandler CreateHandler()
    {
        return new CompleteAnalysisProcessingHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _eventPublisher.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task HandleAsync_Should_Complete_Process_When_Process_Exists()
    {
        var analysisRequestId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var process = CreateStartedProcess(analysisRequestId);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        _mapper
            .Setup(x => x.Map(It.IsAny<AnalysisProcessingCompletedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        var handler = CreateHandler();

        var component = new IdentifiedComponentDto(
            Id: "comp-1",
            Name: "API",
            Type: ComponentType.Backend,
            Description: "Backend API",
            Tags: Array.Empty<string>(),
            ConnectedTo: Array.Empty<string>(),
            Metadata: null);

        var command = new CompleteAnalysisProcessingCommand(
            AnalysisRequestId: analysisRequestId,
            ExtractedText: "texto extraido",
            Components: new[] { component },
            Risks: Array.Empty<ArchitecturalRiskDto>(),
            Recommendations: Array.Empty<ArchitecturalRecommendationDto>(),
            Overview: "Processamento concluído",
            RequiresManualReview: false,
            Warnings: Array.Empty<string>());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error.Message);

        process.Status.Should().Be(ProcessingStatus.Completed);
        process.CompletedAtUtc.Should().Be(now);
        process.Components.Should().HaveCount(1);

        _repository.Verify(x => x.Update(process), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Process_Does_Not_Exist()
    {
        var analysisRequestId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.IsAny<AnalysisRequestId>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisProcess?)null);

        var handler = CreateHandler();

        var command = new CompleteAnalysisProcessingCommand(
            AnalysisRequestId: analysisRequestId,
            ExtractedText: "texto extraido",
            Components: Array.Empty<IdentifiedComponentDto>(),
            Risks: Array.Empty<ArchitecturalRiskDto>(),
            Recommendations: Array.Empty<ArchitecturalRecommendationDto>(),
            Overview: "overview",
            RequiresManualReview: false,
            Warnings: Array.Empty<string>());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _repository.Verify(x => x.Update(It.IsAny<AnalysisProcess>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AnalysisProcess CreateStartedProcess(Guid analysisRequestId)
    {
        var process = AnalysisProcess.Create(
            id: Guid.NewGuid(),
            analysisRequestId: AnalysisRequestId.Create(analysisRequestId),
            requestedByUserId: Guid.NewGuid(),
            sourceFileLocation: SourceFileLocation.Create("analysis-uploads", "uploads/diagram.pdf"),
            diagramType: DiagramType.Pdf,
            createdAtUtc: DateTime.UtcNow);

        process.MarkAsStarted(DateTime.UtcNow);

        return process;
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}