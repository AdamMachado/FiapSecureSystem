using FluentAssertions;
using Moq;
using Shared.Kernel.Result;
using System.Diagnostics;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.Abstractions.Storage;
using UploadService.Application.UseCases.GetAnalysisAsset;
using UploadService.Domain.Entities;
using UploadService.Domain.Enums;
using UploadService.Domain.ValueObjects;

namespace UploadService.Application.Tests.UseCases.GetAnalysisAsset;

public sealed class GetAnalysisAssetHandlerTests
{
    private readonly Mock<IUserContext> _userContext = new();
    private readonly Mock<IAnalysisRequestRepository> _repository = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly ActivitySource _activitySource = new("UploadService.Application.Tests");

    private GetAnalysisAssetHandler CreateHandler()
        => new(
            _userContext.Object,
            _repository.Object,
            _objectStorage.Object,
            _activitySource);

    [Fact]
    public async Task HandleAsync_Should_Return_Asset_Using_Metadata_ContentType_When_Storage_ContentType_Is_Empty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var analysisRequestId = Guid.NewGuid();
        var content = new MemoryStream([1, 2, 3, 4]);
        var analysisRequest = CreateAnalysisRequest(analysisRequestId, userId);

        _userContext
            .Setup(x => x.GetRequiredUserId())
            .Returns(userId);

        _repository
            .Setup(x => x.GetByIdForUserAsync(analysisRequestId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisRequest);

        _objectStorage
            .Setup(x => x.DownloadAsync(
                new DownloadObjectRequest(
                    analysisRequest.StorageLocation.BucketName,
                    analysisRequest.StorageLocation.ObjectKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredObjectContent(
                content,
                string.Empty,
                content.Length,
                "etag-1"));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetAnalysisAssetQuery(analysisRequestId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().BeSameAs(content);
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.FileName.Should().Be("diagram.pdf");
        result.Value.SizeInBytes.Should().Be(content.Length);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_AnalysisRequest_Is_Not_Found_For_User()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var analysisRequestId = Guid.NewGuid();

        _userContext
            .Setup(x => x.GetRequiredUserId())
            .Returns(userId);

        _repository
            .Setup(x => x.GetByIdForUserAsync(analysisRequestId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisRequest?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetAnalysisAssetQuery(analysisRequestId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("analysis_request.not_found");

        _objectStorage.Verify(
            x => x.DownloadAsync(It.IsAny<DownloadObjectRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AnalysisRequest CreateAnalysisRequest(Guid id, Guid requestedByUserId)
    {
        return AnalysisRequest.Create(
            id: id,
            requestedByUserId: requestedByUserId,
            fileMetadata: FileMetadata.Create(
                "diagram.pdf",
                "application/pdf",
                1024,
                FileType.Pdf),
            fileHash: FileHash.Create("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            storageLocation: StorageLocation.Create(
                "analysis-uploads",
                "uploads/diagram.pdf"),
            createdAtUtc: new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc));
    }
}
