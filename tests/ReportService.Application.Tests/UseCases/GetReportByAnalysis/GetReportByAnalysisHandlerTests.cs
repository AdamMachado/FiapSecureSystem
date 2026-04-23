using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.UseCases.GetReportByAnalysis;
using ReportService.Domain.Entities;
using Xunit;

namespace ReportService.Application.Tests.UseCases.GetReportByAnalysis;

public sealed class GetReportByAnalysisHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Report_When_It_Exists()
    {
        var repository = new Mock<IAnalysisReportRepository>();

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<AnalysisReport>());

        var handler = new GetReportByAnalysisHandler(repository.Object);

        var result = await handler.HandleAsync(
            new GetReportByAnalysisQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Report_Does_Not_Exist()
    {
        var repository = new Mock<IAnalysisReportRepository>();

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        var handler = new GetReportByAnalysisHandler(repository.Object);

        var result = await handler.HandleAsync(
            new GetReportByAnalysisQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}