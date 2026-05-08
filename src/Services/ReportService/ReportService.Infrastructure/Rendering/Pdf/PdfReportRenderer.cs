using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReportService.Application.Abstractions.Rendering;
using ReportService.Domain.Enums;
using ReportService.Infrastructure.Exceptions;
using ReportService.Infrastructure.Rendering.Common;

namespace ReportService.Infrastructure.Rendering.Pdf;

public sealed class PdfReportRenderer : IReportRenderer
{
    static PdfReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public bool CanRender(ReportFormat format)
        => format == ReportFormat.Pdf;

    public Task<RenderedReport> RenderAsync(
        RenderReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = AnalysisReportDocumentFactory.Create(request);
            var contentBytes = Document.Create(container => ComposeDocument(container, document)).GeneratePdf();
            var fileName = $"{request.FileNameWithoutExtension}.pdf";

            return Task.FromResult(new RenderedReport(
                FileName: fileName,
                ContentType: "application/pdf",
                Content: contentBytes));
        }
        catch (Exception ex)
        {
            throw new ReportRenderingException("Failed to render report as PDF.", ex);
        }
    }

    private static void ComposeDocument(IDocumentContainer container, ReportDocument document)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(Colors.Grey.Darken3));

            page.Header().Column(header =>
            {
                header.Spacing(6);
                header.Item().Text(document.Title).FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                header.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });

            page.Content().PaddingVertical(10).Column(column =>
            {
                column.Spacing(18);

                foreach (var section in document.Sections)
                    RenderSection(column, section, level: 1);
            });

            page.Footer()
                .AlignCenter()
                .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium))
                .Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
        });
    }

    private static void RenderSection(ColumnDescriptor column, ReportSection section, int level)
    {
        column.Item().Element(container => RenderSectionContainer(container, section, level));
    }

    private static void RenderSectionContainer(IContainer container, ReportSection section, int level)
    {
        var styledContainer = level == 1
            ? container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten5)
                .Padding(14)
            : container
                .PaddingLeft(8)
                .BorderLeft(2)
                .BorderColor(Colors.Blue.Lighten2)
                .PaddingLeft(12);

        styledContainer.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text(section.Title).Style(GetHeadingStyle(level));

            foreach (var block in section.Blocks)
                RenderBlock(column, block, level + 1);

            if (section.Sections is null)
                return;

            foreach (var childSection in section.Sections)
                RenderSection(column, childSection, level + 1);
        });
    }

    private static void RenderBlock(ColumnDescriptor column, IReportBlock block, int level)
    {
        switch (block)
        {
            case ParagraphBlock paragraph:
                RenderParagraph(column, paragraph, level);
                break;
            case TableBlock table:
                RenderTable(column, table, level);
                break;
            case BulletListBlock bulletList:
                RenderBulletList(column, bulletList, level);
                break;
            default:
                throw new InvalidOperationException($"Unsupported report block type '{block.GetType().Name}'.");
        }
    }

    private static void RenderParagraph(ColumnDescriptor column, ParagraphBlock paragraph, int level)
    {
        RenderOptionalBlockTitle(column, paragraph.Title, level);
        column.Item().Text(paragraph.Text);
    }

    private static void RenderTable(ColumnDescriptor column, TableBlock table, int level)
    {
        RenderOptionalBlockTitle(column, table.Title, level);

        if (table.Rows.Count == 0)
        {
            column.Item().Text(table.EmptyState).Italic().FontColor(Colors.Grey.Medium);
            return;
        }

        column.Item().Table(tableDescriptor =>
        {
            tableDescriptor.ColumnsDefinition(columns =>
            {
                foreach (var _ in table.Headers)
                    columns.RelativeColumn();
            });

            tableDescriptor.Header(header =>
            {
                foreach (var cellValue in table.Headers)
                {
                    header.Cell()
                        .Element(StyleTableHeaderCell)
                        .Text(cellValue)
                        .SemiBold();
                }
            });

            foreach (var row in table.Rows)
            {
                foreach (var cellValue in row)
                {
                    tableDescriptor.Cell()
                        .Element(StyleTableBodyCell)
                        .Text(cellValue);
                }
            }
        });
    }

    private static void RenderBulletList(ColumnDescriptor column, BulletListBlock bulletList, int level)
    {
        RenderOptionalBlockTitle(column, bulletList.Title, level);

        if (bulletList.Items.Count == 0)
        {
            column.Item().Text(bulletList.EmptyState).Italic().FontColor(Colors.Grey.Medium);
            return;
        }

        foreach (var item in bulletList.Items)
        {
            column.Item().Row(row =>
            {
                row.Spacing(6);
                row.AutoItem().Text("•").FontColor(Colors.Blue.Medium);
                row.RelativeItem().Text(item);
            });
        }
    }

    private static void RenderOptionalBlockTitle(ColumnDescriptor column, string? title, int level)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        column.Item().Text(title).Style(GetHeadingStyle(level));
    }

    private static TextStyle GetHeadingStyle(int level)
    {
        return level switch
        {
            1 => TextStyle.Default.FontSize(15).SemiBold().FontColor(Colors.Blue.Darken2),
            2 => TextStyle.Default.FontSize(12.5f).SemiBold().FontColor(Colors.Grey.Darken4),
            _ => TextStyle.Default.FontSize(11.5f).SemiBold().FontColor(Colors.Grey.Darken3)
        };
    }

    private static IContainer StyleTableHeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Blue.Lighten4)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(6);
    }

    private static IContainer StyleTableBodyCell(IContainer container)
    {
        return container
            .Background(Colors.White)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(6);
    }
}
