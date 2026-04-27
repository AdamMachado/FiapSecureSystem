using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.UseCases.GetReportByAnalysis;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using Xunit;

namespace ReportService.Application.Tests.UseCases.GetReportByAnalysis;

public sealed class GetReportByAnalysisHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Report_When_It_Exists()
    {
        var repository = new Mock<IAnalysisReportRepository>();

        var analysisRequestId = Guid.NewGuid();

        var report = AnalysisReport.Create(
            id: Guid.NewGuid(),
            analysisRequestId: analysisRequestId,
            requestedByUserId: Guid.NewGuid(),
            format: ReportFormat.Pdf,
            content: "conteudo do relatorio",
            bucketName: "analysis-reports",
            objectKey: "reports/report.pdf",
            fileName: "report.pdf",
            contentType: "application/pdf",
            createdAtUtc: DateTime.UtcNow);

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                analysisRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var handler = new GetReportByAnalysisHandler(repository.Object);

        var result = await handler.HandleAsync(
            new GetReportByAnalysisQuery(analysisRequestId),
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