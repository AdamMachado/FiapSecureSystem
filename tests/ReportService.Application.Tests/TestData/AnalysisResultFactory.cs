using Shared.Contracts.IntegrationEvents.Schemas;

namespace ReportService.Application.Tests.TestData;

internal static class AnalysisResultFactory
{
    public static AnalysisResultDto Create(string overview = "Teste")
    {
        return new AnalysisResultDto(
            Array.Empty<IdentifiedComponentDto>(),
            Array.Empty<ArchitecturalRiskDto>(),
            Array.Empty<ArchitecturalRecommendationDto>(),
            new AnalysisSummaryDto(
                overview,
                0,
                0,
                0,
                false,
                Array.Empty<string>()));
    }
}
