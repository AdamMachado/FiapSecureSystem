using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.UseCases.GetAnalysisStatus;
using UploadService.Domain.Entities;
using UploadService.Domain.Enums;
using UploadService.Domain.ValueObjects;
using Xunit;

namespace UploadService.Application.Tests.UseCases.GetAnalysisStatus;

public sealed class GetAnalysisStatusHandlerTests
{
    private readonly ActivitySource _activitySource = new("UploadService.Application.Tests");
    private readonly Mock<ILogger<GetAnalysisStatusHandler>> _loggerGetStatus = new();


    [Fact]
    public async Task HandleAsync_Should_Return_Status_When_Exists()
    {
        var repository = new Mock<IAnalysisRequestRepository>();

        var analysisRequestId = Guid.NewGuid();

        var analysisRequest = AnalysisRequest.Create(
            id: analysisRequestId,
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

        repository
            .Setup(x => x.GetByIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        var handler = new GetAnalysisStatusHandler(
            repository.Object, 
            _activitySource, 
            _loggerGetStatus.Object);

        var result = await handler.HandleAsync(
            new GetAnalysisStatusQuery(analysisRequestId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var repository = new Mock<IAnalysisRequestRepository>();

        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRequest?)null);

        var handler = new GetAnalysisStatusHandler(
            repository.Object,
            _activitySource,
            _loggerGetStatus.Object);

        var result = await handler.HandleAsync(
            new GetAnalysisStatusQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}