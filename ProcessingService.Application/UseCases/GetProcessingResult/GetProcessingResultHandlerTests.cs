using FluentAssertions;
using Moq;
using ProcessingService.Application.Abstractions.Persistence;
using ProcessingService.Application.UseCases.GetProcessingResult;
using ProcessingService.Domain.Entities;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.ValueObjects;

namespace ProcessingService.Application.Tests.UseCases.GetProcessingResult;

public sealed class GetProcessingResultHandlerTests
{
    private readonly Mock<IAnalysisProcessRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_Should_Return_Result_When_Process_Exists()
    {
        var analysisRequestId = Guid.NewGuid();
        var process = CreateProcess(analysisRequestId);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                It.Is<AnalysisRequestId>(id => id.Value == analysisRequestId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(process);

        var handler = new GetProcessingResultHandler(_repository.Object);

        var result = await handler.HandleAsync(
            new GetProcessingResultQuery(analysisRequestId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AnalysisRequestId.Should().Be(analysisRequestId);
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

        var handler = new GetProcessingResultHandler(_repository.Object);

        var result = await handler.HandleAsync(
            new GetProcessingResultQuery(analysisRequestId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _repository.Verify(
            x => x.GetByAnalysisRequestIdAsync(
                It.IsAny<AnalysisRequestId>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        return process;
    }
}