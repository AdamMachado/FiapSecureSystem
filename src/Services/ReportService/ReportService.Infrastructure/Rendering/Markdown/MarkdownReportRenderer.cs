using System.Text;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Domain.Enums;
using ReportService.Infrastructure.Exceptions;
using ReportService.Infrastructure.Rendering.Common;

namespace ReportService.Infrastructure.Rendering.Markdown;

public sealed class MarkdownReportRenderer : IReportRenderer
{
    public bool CanRender(ReportFormat format)
        => format == ReportFormat.Markdown;

    public Task<RenderedReport> RenderAsync(
        RenderReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = AnalysisReportDocumentFactory.Create(request);
            var markdown = RenderDocument(document);
            var contentBytes = Encoding.UTF8.GetBytes(markdown);
            var fileName = $"{request.FileNameWithoutExtension}.md";

            return Task.FromResult(new RenderedReport(
                FileName: fileName,
                ContentType: "text/markdown",
                Content: contentBytes));
        }
        catch (Exception ex)
        {
            throw new ReportRenderingException("Failed to render report as Markdown.", ex);
        }
    }

    private static string RenderDocument(ReportDocument document)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# {document.Title}");
        builder.AppendLine();

        foreach (var section in document.Sections)
            RenderSection(builder, section, level: 2);

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static void RenderSection(StringBuilder builder, ReportSection section, int level)
    {
        builder.AppendLine($"{new string('#', level)} {section.Title}");
        builder.AppendLine();

        foreach (var block in section.Blocks)
            RenderBlock(builder, block, level + 1);

        if (section.Sections is null)
            return;

        foreach (var childSection in section.Sections)
            RenderSection(builder, childSection, level + 1);
    }

    private static void RenderBlock(StringBuilder builder, IReportBlock block, int level)
    {
        switch (block)
        {
            case ParagraphBlock paragraph:
                RenderParagraph(builder, paragraph, level);
                break;
            case TableBlock table:
                RenderTable(builder, table, level);
                break;
            case BulletListBlock bulletList:
                RenderBulletList(builder, bulletList, level);
                break;
            default:
                throw new InvalidOperationException($"Unsupported report block type '{block.GetType().Name}'.");
        }
    }

    private static void RenderParagraph(StringBuilder builder, ParagraphBlock paragraph, int level)
    {
        RenderOptionalBlockTitle(builder, paragraph.Title, level);
        builder.AppendLine(paragraph.Text);
        builder.AppendLine();
    }

    private static void RenderTable(StringBuilder builder, TableBlock table, int level)
    {
        RenderOptionalBlockTitle(builder, table.Title, level);

        if (table.Rows.Count == 0)
        {
            builder.AppendLine($"_{table.EmptyState}_");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"| {string.Join(" | ", table.Headers.Select(EscapeMarkdownCell))} |");
        builder.AppendLine($"| {string.Join(" | ", table.Headers.Select(_ => "---"))} |");

        foreach (var row in table.Rows)
            builder.AppendLine($"| {string.Join(" | ", row.Select(EscapeMarkdownCell))} |");

        builder.AppendLine();
    }

    private static void RenderBulletList(StringBuilder builder, BulletListBlock bulletList, int level)
    {
        RenderOptionalBlockTitle(builder, bulletList.Title, level);

        if (bulletList.Items.Count == 0)
        {
            builder.AppendLine($"_{bulletList.EmptyState}_");
            builder.AppendLine();
            return;
        }

        foreach (var item in bulletList.Items)
            builder.AppendLine($"- {item}");

        builder.AppendLine();
    }

    private static void RenderOptionalBlockTitle(StringBuilder builder, string? title, int level)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        builder.AppendLine($"{new string('#', level)} {title}");
        builder.AppendLine();
    }

    private static string EscapeMarkdownCell(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);
}
