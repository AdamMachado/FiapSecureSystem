using FluentAssertions;
using Moq;
using Shared.Kernel.Pagination;
using System.Diagnostics;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.UseCases.ListUserAnalysisRequests;
using UploadService.Domain.Entities;
using UploadService.Domain.Enums;
using UploadService.Domain.ValueObjects;

namespace UploadService.Application.Tests.UseCases.ListUserAnalysisRequests;

public sealed class ListUserAnalysisRequestsHandlerTests
{
    private readonly Mock<IUserContext> _userContext = new();
    private readonly Mock<IAnalysisRequestRepository> _repository = new();
    private readonly ActivitySource _activitySource = new("UploadService.Application.Tests");

    private ListUserAnalysisRequestsHandler CreateHandler()
        => new(
            _userContext.Object,
            _repository.Object,
            _activitySource);

    [Fact]
    public async Task HandleAsync_Should_Return_Mapped_Paged_Result()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paginationParams = new PaginationParams
        {
            PageNumber = 2,
            PageSize = 5
        };

        var firstRequest = CreateAnalysisRequest(Guid.NewGuid(), userId, "first.pdf", AnalysisStatus.Processing);
        var secondRequest = CreateAnalysisRequest(Guid.NewGuid(), userId, "second.pdf", AnalysisStatus.Completed);

        _userContext
            .Setup(x => x.GetRequiredUserId())
            .Returns(userId);

        _repository
            .Setup(x => x.ListByUserAsync(userId, paginationParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<AnalysisRequest>.Create(
                [firstRequest, secondRequest],
                totalCount: 9,
                pageNumber: paginationParams.PageNumber,
                pageSize: paginationParams.PageSize));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new ListUserAnalysisRequestsQuery(paginationParams),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(9);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(x => x.FileName).Should().ContainInOrder("first.pdf", "second.pdf");
        result.Value.Items.Select(x => x.Status).Should().ContainInOrder(AnalysisStatus.Processing, AnalysisStatus.Completed);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_Paged_Result_When_User_Has_No_AnalysisRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paginationParams = new PaginationParams
        {
            PageNumber = 3,
            PageSize = 10
        };

        _userContext
            .Setup(x => x.GetRequiredUserId())
            .Returns(userId);

        _repository
            .Setup(x => x.ListByUserAsync(userId, paginationParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<AnalysisRequest>.Empty(
                paginationParams.PageNumber,
                paginationParams.PageSize));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new ListUserAnalysisRequestsQuery(paginationParams),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.PageNumber.Should().Be(3);
        result.Value.PageSize.Should().Be(10);
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
