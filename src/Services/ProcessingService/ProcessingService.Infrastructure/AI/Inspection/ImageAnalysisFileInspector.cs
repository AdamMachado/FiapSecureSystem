using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Infrastructure.AI.Options;
using SixLabors.ImageSharp;

namespace ProcessingService.Infrastructure.AI.Inspection;

public sealed class ImageAnalysisFileInspector : IAnalysisFileInspector
{
    private readonly ArchitectureAnalysisOptions _options;
    private readonly ILogger<ImageAnalysisFileInspector> _logger;

    public ImageAnalysisFileInspector(IOptions<ArchitectureAnalysisOptions> options, ILogger<ImageAnalysisFileInspector> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool CanInspect(DiagramType diagramType)
    {
        return diagramType is DiagramType.Image;
    }

    public Task<AnalysisFileInspectionResult> InspectAsync(
        ArchitectureAnalysisRequest request,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Inspecting image file for architecture analysis. DiagramType: {DiagramType}, ContentType: {ContentType}, Size: {Size} bytes",
            request.DiagramType,
            request.ContentType,
            content.Length);

        ImageInfo imageInfo;

        try
        {
            imageInfo = Image.Identify(content) ?? throw new DiagramProcessingException("The image could not be decoded.");
        }
        catch (UnknownImageFormatException ex)
        {
            throw new DiagramProcessingException("The source image format is not recognized or is corrupted.", ex);
        }
        catch (InvalidImageContentException ex)
        {
            throw new DiagramProcessingException("The source image content is invalid or corrupted.", ex);
        }

        var warnings = new List<string>();

        if (imageInfo.Width < _options.MinImageWidth || imageInfo.Height < _options.MinImageHeight)
            throw new DiagramProcessingException($"The image resolution is too small for reliable AI analysis. Minimum: {_options.MinImageWidth}x{_options.MinImageHeight}. Provided: {imageInfo.Width}x{imageInfo.Height}.");

        if (imageInfo.Width > _options.MaxImageWidth || imageInfo.Height > _options.MaxImageHeight)
            throw new DiagramProcessingException($"The image resolution exceeds the maximum allowed for AI analysis. Maximum: {_options.MaxImageWidth}x{_options.MaxImageHeight}. Provided: {imageInfo.Width}x{imageInfo.Height}.");

        var aspectRatio = imageInfo.Width / (double)Math.Max(imageInfo.Height, 1);
        if (aspectRatio is > 6.0 or < 0.16)
            warnings.Add("The image aspect ratio is unusual and may reduce analysis quality.");

        _logger.LogInformation(
            "Completed inspection of image file. DiagramType: {DiagramType}, ContentType: {ContentType}, Size: {Size} bytes, Width: {Width}px, Height: {Height}px, Warnings: {WarningsCount}",
            request.DiagramType,
            request.ContentType,
            content.Length,
            imageInfo.Width,
            imageInfo.Height,
            warnings.Count);

        return Task.FromResult(new AnalysisFileInspectionResult(
            request.DiagramType,
            request.ContentType,
            content.Length,
            imageInfo.Width,
            imageInfo.Height,
            PageCount: null,
            IsEncrypted: null,
            ExtractedTextPreview: null,
            warnings));
    }
}
