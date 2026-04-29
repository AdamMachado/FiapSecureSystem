using FluentAssertions;
using Moq;
using UploadService.Application.Abstractions.Clock;
using UploadService.Application.Abstractions.Identity;
using UploadService.Application.Abstractions.Persistence;
using UploadService.Application.Abstractions.Storage;
using UploadService.Application.Abstractions.Messaging;
using UploadService.Application.UseCases.CreateAnalysis;
using UploadService.Domain.Events;
using UploadService.Domain.ValueObjects;
using Shared.Contracts.IntegrationEvents.Abstractions;
using Shared.Kernel.Result;
using Xunit;

namespace UploadService.Application.Tests.UseCases.CreateAnalysis;

public sealed class CreateAnalysisHandlerTests
{
    private readonly Mock<IUserContext> _userContext = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly Mock<IStorageObjectKeyFactory> _keyFactory = new();
    private readonly Mock<IUploadPolicy> _uploadPolicy = new();
    private readonly Mock<IAnalysisRequestRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IIntegrationEventMapper<AnalysisRequestCreatedDomainEvent>> _mapper = new();

    private CreateAnalysisHandler CreateHandler()
    {
        return new CreateAnalysisHandler(
            new CreateAnalysisValidator(),
            _userContext.Object,
            _dateTimeProvider.Object,
            _objectStorage.Object,
            _keyFactory.Object,
            _uploadPolicy.Object,
            _repository.Object,
            _unitOfWork.Object,
            _eventPublisher.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Analysis_Successfully()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);
        _userContext.Setup(x => x.GetRequiredUserId()).Returns(userId);

        _uploadPolicy
            .Setup(x => x.IsContentTypeSupported("application/pdf"))
            .Returns(true);

        _uploadPolicy
            .Setup(x => x.ResolveFileType("application/pdf"))
            .Returns(UploadService.Domain.Enums.FileType.Pdf);

        _keyFactory
            .Setup(x => x.CreateForAnalysisUpload(
                It.IsAny<Guid>(),
                "diagram.pdf",
                now))
            .Returns("uploads/test-key.pdf");

        _objectStorage
            .Setup(x => x.UploadAsync(
                It.IsAny<UploadObjectRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredObjectDescriptor(
                StorageLocation.Create("bucket", "uploads/test-key.pdf"),
                "etag-test"
            ));

        _mapper
            .Setup(x => x.Map(It.IsAny<AnalysisRequestCreatedDomainEvent>()))
            .Returns(new FakeIntegrationEvent());

        var handler = CreateHandler();

        var command = new CreateAnalysisCommand(
            FileName: "diagram.pdf",
            ContentType: "application/pdf",
            SizeInBytes: 3,
            Content: stream,
            FileHash: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        );

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _repository.Verify(
            x => x.AddAsync(It.IsAny<UploadService.Domain.Entities.AnalysisRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _eventPublisher.Verify(
            x => x.PublishAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_ContentType_Is_Not_Supported()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _uploadPolicy
            .Setup(x => x.IsContentTypeSupported(It.IsAny<string>()))
            .Returns(false);

        var handler = CreateHandler();

        var command = new CreateAnalysisCommand(
            "file.exe",
            "application/exe",
            3,
            stream,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        );

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _repository.Verify(
            x => x.AddAsync(It.IsAny<UploadService.Domain.Entities.AnalysisRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class FakeIntegrationEvent : IntegrationEventBase
    {
        public FakeIntegrationEvent() : base(Guid.NewGuid(), null)
        {
        }
    }
}