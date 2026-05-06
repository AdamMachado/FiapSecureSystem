using FluentAssertions;
using Moq;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.IntegrationEvents.Schemas;
using System.Diagnostics;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.Integration.Consumed;
using UploadService.Application.UseCases.UpdateAnalysisStatus;
using UploadService.Domain.Entities;
using UploadService.Domain.Enums;
using UploadService.Domain.ValueObjects;
using Xunit;

namespace UploadService.Application.Tests.Integration.Consumed;

public sealed class AnalysisMessageHandlersTests
{
    private readonly Mock<IAnalysisRequestRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly ActivitySource _activitySource = new("UploadService.Application.Tests");

    private UpdateAnalysisStatusHandler CreateUpdateHandler()
    {
        return new UpdateAnalysisStatusHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _activitySource);
    }

    [Fact]
    public async Task AnalysisStartedMessageHandler_Should_Update_Status_To_Processing()
    {
        var analysisRequestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var analysisRequest = CreateAnalysisRequest(analysisRequestId);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        _repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        var handler = new AnalysisStartedMessageHandler(CreateUpdateHandler());

        var integrationEvent = new AnalysisStartedIntegrationEvent(
            Guid.NewGuid(),
            null,
            analysisRequestId,
            userId,
            now);

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        analysisRequest.Status.Should().Be(AnalysisStatus.Processing);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalysisCompletedMessageHandler_Should_Update_Status_To_Completed()
    {
        var analysisRequestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var analysisRequest = CreateAnalysisRequest(analysisRequestId);
        analysisRequest.MarkAsProcessing(now.AddMinutes(-1));

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        _repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        var handler = new AnalysisCompletedMessageHandler(CreateUpdateHandler());

        var integrationEvent = new AnalysisCompletedIntegrationEvent(
            Guid.NewGuid(),
            null,
            analysisRequestId,
            userId,
            now,
            new AnalysisResultDto(
                Array.Empty<IdentifiedComponentDto>(),
                Array.Empty<ArchitecturalRiskDto>(),
                Array.Empty<ArchitecturalRecommendationDto>(),
                new AnalysisSummaryDto(
                    "ok",
                    0,
                    0,
                    0,
                    false,
                    Array.Empty<string>())));

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        analysisRequest.Status.Should().Be(AnalysisStatus.Completed);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalysisFailedMessageHandler_Should_Update_Status_To_Failed()
    {
        var analysisRequestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var analysisRequest = CreateAnalysisRequest(analysisRequestId);
        var reason = "AI analysis failed";

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        _repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        var handler = new AnalysisFailedMessageHandler(CreateUpdateHandler());

        var integrationEvent = new AnalysisFailedIntegrationEvent(
            Guid.NewGuid(),
            null,
            analysisRequestId,
            userId,
            now,
            reason,
            "details");

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        analysisRequest.Status.Should().Be(AnalysisStatus.Failed);
        analysisRequest.FailureReason.Should().Be(reason);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AnalysisRequest CreateAnalysisRequest(Guid id)
    {
        return AnalysisRequest.Create(
            id: id,
            requestedByUserId: Guid.NewGuid(),
            fileMetadata: FileMetadata.Create(
                "diagram.pdf",
                "application/pdf",
                1024,
                FileType.Pdf),
            fileHash: FileHash.Create("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            storageLocation: StorageLocation.Create(
                "analysis-uploads",
                "uploads/diagram.pdf"),
            createdAtUtc: DateTime.UtcNow);
    }
}