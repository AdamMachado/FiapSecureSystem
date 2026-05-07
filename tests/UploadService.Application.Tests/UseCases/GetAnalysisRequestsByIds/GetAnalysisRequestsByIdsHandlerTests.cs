using FluentAssertions;
using Moq;
using System.Diagnostics;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.UseCases.GetAnalysisRequestsByIds;
using UploadService.Domain.Entities;
using UploadService.Domain.Enums;
using UploadService.Domain.ValueObjects;

namespace UploadService.Application.Tests.UseCases.GetAnalysisRequestsByIds;

public sealed class GetAnalysisRequestsByIdsHandlerTests
{
    private readonly Mock<IUserContext> _userContext = new();
    private readonly Mock<IAnalysisRequestRepository> _repository = new();
    private readonly ActivitySource _activitySource = new("UploadService.Application.Tests");

    private GetAnalysisRequestsByIdsHandler CreateHandler()
        => new(
            _userContext.Object,
            _repository.Object,
            _activitySource);

    [Fact]
    public async Task HandleAsync_Should_Return_AnalysisRequests_Preserving_Requested_Order_And_Ignoring_Missing_Ids()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        var requestedIds = new[] { secondId, Guid.Empty, missingId, firstId, secondId };

        var firstRequest = CreateAnalysisRequest(firstId, userId, "first.pdf", AnalysisStatus.Completed);
        var secondRequest = CreateAnalysisRequest(secondId, userId, "second.pdf", AnalysisStatus.Processing);

        _userContext
            .Setup(x => x.GetRequiredUserId())
            .Returns(userId);

        _repository
            .Setup(x => x.ListByUserAndIdsAsync(userId, requestedIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstRequest, secondRequest]);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetAnalysisRequestsByIdsQuery(requestedIds),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(x => x.AnalysisRequestId).Should().ContainInOrder(secondId, firstId);
        result.Value.Select(x => x.FileName).Should().ContainInOrder("second.pdf", "first.pdf");
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_Repository_Finds_No_AnalysisRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestedIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        _userContext
            .Setup(x => x.GetRequiredUserId())
            .Returns(userId);

        _repository
            .Setup(x => x.ListByUserAndIdsAsync(userId, requestedIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AnalysisRequest>());

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetAnalysisRequestsByIdsQuery(requestedIds),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    private static AnalysisRequest CreateAnalysisRequest(
        Guid id,
        Guid requestedByUserId,
        string fileName,
        AnalysisStatus targetStatus)
    {
        var createdAtUtc = new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);

        var analysisRequest = AnalysisRequest.Create(
            id: id,
            requestedByUserId: requestedByUserId,
            fileMetadata: FileMetadata.Create(
                fileName,
                "application/pdf",
                1024,
                FileType.Pdf),
            fileHash: FileHash.Create("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            storageLocation: StorageLocation.Create(
                "analysis-uploads",
                $"uploads/{fileName}"),
            createdAtUtc: createdAtUtc);

        if (targetStatus == AnalysisStatus.Processing)
            analysisRequest.MarkAsProcessing(createdAtUtc.AddMinutes(1));

        if (targetStatus == AnalysisStatus.Completed)
        {
            analysisRequest.MarkAsProcessing(createdAtUtc.AddMinutes(1));
            analysisRequest.MarkAsCompleted(createdAtUtc.AddMinutes(2));
        }

        if (targetStatus == AnalysisStatus.Failed)
            analysisRequest.MarkAsFailed("failure", createdAtUtc.AddMinutes(1));

        return analysisRequest;
    }
}
