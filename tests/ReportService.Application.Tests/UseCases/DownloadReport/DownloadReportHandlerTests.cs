using FluentAssertions;
using Moq;
using ReportService.Application.Abstractions.Persistence;
using ReportService.Application.Abstractions.Storage;
using ReportService.Application.UseCases.DownloadReport;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using System.Diagnostics;
using Xunit;

namespace ReportService.Application.Tests.UseCases.DownloadReport;

public sealed class DownloadReportHandlerTests
{
    private readonly Mock<IAnalysisReportRepository> _repository = new();
    private readonly Mock<IReportStorage> _storage = new();
    private readonly ActivitySource _activitySource = new("ReportService.Application.Tests");

    private DownloadReportHandler CreateHandler()
    {
        return new DownloadReportHandler(
            _repository.Object,
            _storage.Object,
            _activitySource);
    }
    [Fact]
    public async Task HandleAsync_Should_Return_File_When_Report_And_Object_Exist()
    {
        var reportId = Guid.NewGuid();
        var analysisRequestId = Guid.NewGuid();

        var report = AnalysisReport.Create(
            id: reportId,
            analysisRequestId: analysisRequestId,
            requestedByUserId: Guid.NewGuid(),
            format: ReportFormat.Pdf,
            content: "conteudo do relatorio",
            bucketName: "analysis-reports",
            objectKey: "reports/report.pdf",
            fileName: "report.pdf",
            contentType: "application/pdf",
            createdAtUtc: DateTime.UtcNow);

        _repository
            .Setup(x => x.GetByAnalysisRequestIdAsync(
                analysisRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _storage
            .Setup(x => x.DownloadAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadedReportDescriptor(
                "report.pdf",
                "application/pdf",
                new MemoryStream(new byte[] { 1, 2, 3 })));

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new DownloadReportQuery(analysisRequestId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Error.Message);
        result.Value.FileName.Should().Be("report.pdf");
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.Content.Should().NotBeNull();

        _repository.Verify(
            x => x.GetByAnalysisRequestIdAsync(
                analysisRequestId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _storage.Verify(
            x => x.DownloadAsync(
                "analysis-reports",
                "reports/report.pdf",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Report_Does_Not_Exist()
    {
        var reportId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalysisReport?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new DownloadReportQuery(reportId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _storage.Verify(
            x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Object_Does_Not_Exist_In_Storage()
    {
        var reportId = Guid.NewGuid();

        var report = AnalysisReport.Create(
            id: reportId,
            analysisRequestId: Guid.NewGuid(),
            requestedByUserId: Guid.NewGuid(),
            format: ReportService.Domain.Enums.ReportFormat.Pdf,
            content: "conteudo do relatorio",
            bucketName: "analysis-reports",
            objectKey: "reports/report.pdf",
            fileName: "report.pdf",
            contentType: "application/pdf",
            createdAtUtc: DateTime.UtcNow);

        _repository
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _storage
            .Setup(x => x.DownloadAsync(
                "analysis-reports",
                "reports/report.pdf",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DownloadedReportDescriptor?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new DownloadReportQuery(reportId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}