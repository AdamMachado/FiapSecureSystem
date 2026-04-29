using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Infrastructure.AI.Options;
using UglyToad.PdfPig;

namespace ProcessingService.Infrastructure.AI.Inspection;

public sealed class PdfAnalysisFileInspector : IAnalysisFileInspector
{
    private readonly ArchitectureAnalysisOptions _options;
    private readonly ILogger<PdfAnalysisFileInspector> _logger;

    public PdfAnalysisFileInspector(IOptions<ArchitectureAnalysisOptions> options, ILogger<PdfAnalysisFileInspector> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool CanInspect(DiagramType diagramType)
    {
        return diagramType == DiagramType.Pdf;
    }

    public Task<AnalysisFileInspectionResult> InspectAsync(
        ArchitectureAnalysisRequest request,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Inspecting PDF file for architecture analysis. DiagramType: {DiagramType}, ContentType: {ContentType}, Size: {Size} bytes",
            request.DiagramType,
            request.ContentType,
            content.Length);

        var warnings = new List<string>();

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var document = PdfDocument.Open(stream);

            var pageCount = document.NumberOfPages;
            if (pageCount <= 0)
                throw new DiagramProcessingException("The PDF does not contain pages.");

            if (pageCount > _options.MaxPdfPages)
                throw new DiagramProcessingException($"The PDF has too many pages for the configured AI analysis policy. Maximum: {_options.MaxPdfPages}. Provided: {pageCount}.");

            string? textPreview = null;

            if (_options.EnablePdfTextPreExtraction)
            {
                var textParts = new List<string>();

                foreach (var page in document.GetPages().Take(_options.MaxPdfPages))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var text = page.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                        textParts.Add(text.Trim());
                }

                textPreview = string.Join(Environment.NewLine, textParts);

                if (textPreview.Length > _options.ExtractedTextPreviewMaxLength)
                    textPreview = textPreview[.._options.ExtractedTextPreviewMaxLength];

                if (textPreview.Trim().Length < _options.MinExtractedTextLengthForPdf)
                    warnings.Add("The PDF has little or no extractable text. The model will rely mostly on visual page analysis.");
            }

            _logger.LogInformation(
                "Completed PDF inspection. DiagramType: {DiagramType}, ContentType: {ContentType}, Size: {Size} bytes, PageCount: {PageCount}, TextPreviewLength: {TextPreviewLength} characters, Warnings: {WarningCount}",
                request.DiagramType,
                request.ContentType,
                content.Length,
                pageCount,
                textPreview?.Length ?? 0,
                warnings.Count);

            return Task.FromResult(new AnalysisFileInspectionResult(
                request.DiagramType,
                request.ContentType,
                content.Length,
                Width: null,
                Height: null,
                pageCount,
                IsEncrypted: false,
                textPreview,
                warnings));
        }
        catch (Exception ex) when (IsPdfPasswordOrEncryptionException(ex) && _options.RejectEncryptedPdf)
        {
            throw new DiagramProcessingException("Encrypted or password-protected PDFs are not supported for AI analysis.", ex);
        }
        catch (Exception ex)
        {
            throw new DiagramProcessingException("The PDF is invalid, corrupted, encrypted, or unsupported by the local PDF inspector.", ex);
        }
    }

    private static bool IsPdfPasswordOrEncryptionException(Exception ex)
    {
        var typeName = ex.GetType().Name;
        return typeName.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Encrypted", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Encryption", StringComparison.OrdinalIgnoreCase);
    }
}
