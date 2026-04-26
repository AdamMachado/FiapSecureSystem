using ProcessingService.Domain.Enums;

namespace ProcessingService.Infrastructure.AI.Inspection;

public sealed record AnalysisFileInspectionResult(
    DiagramType DiagramType,
    string ContentType,
    long SizeInBytes,
    int? Width,
    int? Height,
    int? PageCount,
    bool? IsEncrypted,
    string? ExtractedTextPreview,
    IReadOnlyCollection<string> Warnings)
{
    public bool HasExtractedText => !string.IsNullOrWhiteSpace(ExtractedTextPreview);
}
