namespace ProcessingService.Infrastructure.AI.Options;

public sealed class ArchitectureAnalysisOptions
{
    public const string SectionName = "ArchitectureAnalysis";

    public string PromptVersion { get; init; } = "architecture-analysis-v1";
    public string Language { get; init; } = "pt-BR";
    public string ImageDetail { get; init; } = "auto";

    public long MinFileSizeInBytes { get; init; } = 1_024; // 1 KB
    public long MaxFileSizeInBytes { get; init; } = 10 * 1024 * 1024; // 10 MB

    public int MinImageWidth { get; init; } = 300;
    public int MinImageHeight { get; init; } = 300;
    public int MaxImageWidth { get; init; } = 6_000;
    public int MaxImageHeight { get; init; } = 6_000;

    public int MaxPdfPages { get; init; } = 5;
    public int MinExtractedTextLengthForPdf { get; init; } = 10;
    public int ExtractedTextPreviewMaxLength { get; init; } = 4_000;

    public bool RejectEncryptedPdf { get; init; } = true;
    public bool EnablePdfTextPreExtraction { get; init; } = true;
    public bool EnableDiagramHeuristics { get; init; } = true;

    public int MaxComponents { get; init; } = 50;
    public int MaxRisks { get; init; } = 30;
    public int MaxRecommendations { get; init; } = 30;
    public int MaxWarnings { get; init; } = 20;
    public int MaxDescriptionLength { get; init; } = 1_500;

    public bool RequireEvidenceForRisks { get; init; } = true;
    public bool UseCompactOutput { get; init; } = true;
}
