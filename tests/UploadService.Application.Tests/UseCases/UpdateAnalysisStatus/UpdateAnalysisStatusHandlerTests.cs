using FluentAssertions;
using Microsoft.VisualBasic.FileIO;
using Moq;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.UseCases.UpdateAnalysisStatus;
using UploadService.Domain.Entities;
using UploadService.Domain.Enums;
using UploadService.Domain.ValueObjects;
using Xunit;

namespace UploadService.Application.Tests.UseCases.UpdateAnalysisStatus;

public sealed class UpdateAnalysisStatusHandlerTests
{
    private readonly Mock<IAnalysisRequestRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly ActivitySource _activitySource = new("UploadService.Application.Tests");

    private UpdateAnalysisStatusHandler CreateHandler()
    {
        return new UpdateAnalysisStatusHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _activitySource);
    }

    [Fact]
    public async Task HandleAsync_Should_Mark_As_Processing_When_TargetStatus_Is_Processing()
    {
        var analysisRequestId = Guid.NewGuid();
        var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        var analysisRequest = CreateAnalysisRequest(analysisRequestId);

        _repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new UpdateAnalysisStatusCommand(analysisRequestId, AnalysisStatus.Processing),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        analysisRequest.Status.Should().Be(AnalysisStatus.Processing);
        analysisRequest.StartedAtUtc.Should().Be(now);

        _repository.Verify(x => x.Update(analysisRequest), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Mark_As_Completed_When_TargetStatus_Is_Completed()
    {
        var analysisRequestId = Guid.NewGuid();
        var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        var analysisRequest = CreateAnalysisRequest(analysisRequestId);
        analysisRequest.MarkAsProcessing(now.AddMinutes(-5));

        _repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new UpdateAnalysisStatusCommand(analysisRequestId, AnalysisStatus.Completed),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        analysisRequest.Status.Should().Be(AnalysisStatus.Completed);
        analysisRequest.CompletedAtUtc.Should().Be(now);

        _repository.Verify(x => x.Update(analysisRequest), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Mark_As_Failed_When_TargetStatus_Is_Failed()
    {
        var analysisRequestId = Guid.NewGuid();
        var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        var analysisRequest = CreateAnalysisRequest(analysisRequestId);

        _repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new UpdateAnalysisStatusCommand(
                analysisRequestId,
                AnalysisStatus.Failed,
                "AI processing failed"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        analysisRequest.Status.Should().Be(AnalysisStatus.Failed);
        analysisRequest.FailureReason.Should().Be("AI processing failed");
        analysisRequest.FailedAtUtc.Should().Be(now);

        _repository.Verify(x => x.Update(analysisRequest), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_AnalysisRequest_Does_Not_Exist()
    {
        var analysisRequestId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRequest?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new UpdateAnalysisStatusCommand(analysisRequestId, AnalysisStatus.Completed),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _repository.Verify(
            x => x.Update(It.IsAny<AnalysisRequest>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
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