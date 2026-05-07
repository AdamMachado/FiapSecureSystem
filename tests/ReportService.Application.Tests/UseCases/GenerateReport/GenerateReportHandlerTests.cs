using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Clock;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Mappings;
using ReportService.Application.Tests.TestData;
using ReportService.Application.UseCases.GenerateReport;
using ReportService.Domain.Entities;
using System.Diagnostics;
using Xunit;

namespace ReportService.Application.Tests.UseCases.GenerateReport;

public sealed class GenerateReportHandlerTests
{
    private readonly Mock<IAnalysisReportRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly ActivitySource _activitySource = new("ReportService.Application.Tests");

    private GenerateReportHandler CreateHandler()
    {
        return new GenerateReportHandler(
            new GenerateReportValidator(),
            _repository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            _activitySource);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Base_Report_When_Command_Is_Valid()
    {
        var now = new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc);
        var command = new GenerateReportCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnalysisResultFactory.Create());

        _dateTimeProvider.Setup(x => x.UtcNow).Returns(now);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(command.AnalysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AnalysisRequestId.Should().Be(command.AnalysisRequestId);
        result.Value.RequestedByUserId.Should().Be(command.RequestedByUserId);

        _repository.Verify(
            x => x.AddAsync(
                It.Is<AnalysisReport>(report =>
                    report.AnalysisRequestId == command.AnalysisRequestId &&
                    report.RequestedByUserId == command.RequestedByUserId &&
                    report.AnalysisData == AnalysisReportMappings.ToAnalysisJson(command.Result) &&
                    report.Files.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Existing_Report_When_Base_Report_Already_Exists()
    {
        var existingReport = AnalysisReport.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnalysisReportMappings.ToAnalysisJson(AnalysisResultFactory.Create()),
            DateTime.UtcNow);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(existingReport.AnalysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReport);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new GenerateReportCommand(
                existingReport.AnalysisRequestId,
                existingReport.RequestedByUserId,
                AnalysisResultFactory.Create()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReportId.Should().Be(existingReport.Id);

        _repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Command_Is_Invalid()
    {
        var handler = CreateHandler();

        var invalidCommand = new GenerateReportCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AnalysisResultFactory.Create(string.Empty));

        var result = await handler.HandleAsync(invalidCommand, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _repository.Verify(
            x => x.AddAsync(It.IsAny<AnalysisReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
