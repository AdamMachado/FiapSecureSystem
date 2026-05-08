namespace ReportService.Infrastructure.Rendering.Common;

internal sealed record ReportDocument(
    string Title,
    IReadOnlyCollection<ReportSection> Sections);

internal sealed record ReportSection(
    string Title,
    IReadOnlyCollection<IReportBlock> Blocks,
    IReadOnlyCollection<ReportSection>? Sections = null);

internal interface IReportBlock
{
    string? Title { get; }
}

internal sealed record ParagraphBlock(string Text, string? Title = null) : IReportBlock;

internal sealed record BulletListBlock(
    IReadOnlyCollection<string> Items,
    string EmptyState,
    string? Title = null) : IReportBlock;

internal sealed record TableBlock(
    IReadOnlyCollection<string> Headers,
    IReadOnlyCollection<IReadOnlyCollection<string>> Rows,
    string EmptyState,
    string? Title = null) : IReportBlock;
