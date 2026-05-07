using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Mappings;
using ReportService.Application.Tests.TestData;
using ReportService.Application.UseCases.GetReportByAnalysis;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using System.Diagnostics;
using Xunit;

namespace ReportService.Application.Tests.UseCases.GetReportByAnalysis;

public sealed class GetReportByAnalysisHandlerTests
{
    private readonly ActivitySource _activitySource = new("ReportService.Application.Tests");

    [Fact]
    public async Task HandleAsync_Should_Return_Report_With_File_Metadata_When_It_Exists()
    {
        var repository = new Mock<IAnalysisReportRepository>();
        var analysisRequestId = Guid.NewGuid();

        var report = AnalysisReport.Create(
            id: Guid.NewGuid(),
            analysisRequestId: analysisRequestId,
            requestedByUserId: Guid.NewGuid(),
            analysisData: AnalysisReportMappings.ToAnalysisJson(AnalysisResultFactory.Create()),
            createdAtUtc: DateTime.UtcNow);

        report.AddFile(
            Guid.NewGuid(),
            ReportFormat.Pdf,
            "analysis-reports",
            "reports/report.pdf",
            "report.pdf",
            "application/pdf",
            DateTime.UtcNow);

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(analysisRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var handler = new GetReportByAnalysisHandler(repository.Object, _activitySource);

        var result = await handler.HandleAsync(
            new GetReportByAnalysisQuery(analysisRequestId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().ContainSingle(x => x.Format == ReportFormat.Pdf);
        result.Value.AnalysisData.TryGetProperty("summary", out _).Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Report_Does_Not_Exist()
    {
        var repository = new Mock<IAnalysisReportRepository>();

        repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        var handler = new GetReportByAnalysisHandler(repository.Object, _activitySource);

        var result = await handler.HandleAsync(
            new GetReportByAnalysisQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
