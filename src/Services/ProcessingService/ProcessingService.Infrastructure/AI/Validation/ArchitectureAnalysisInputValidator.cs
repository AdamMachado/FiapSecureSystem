using Microsoft.Extensions.Options;
using ProcessingService.Application.Abstractions.AI;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Infrastructure.AI.Options;
using Shared.Kernel.Exceptions;

namespace ProcessingService.Infrastructure.AI.Validation;

public sealed class ArchitectureAnalysisInputValidator
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg"
    };

    private readonly ArchitectureAnalysisOptions _options;
    private readonly FileSignatureValidator _signatureValidator;

    public ArchitectureAnalysisInputValidator(
        IOptions<ArchitectureAnalysisOptions> options,
        FileSignatureValidator signatureValidator)
    {
        _options = options.Value;
        _signatureValidator = signatureValidator;
    }

    public void ValidateAndThrow(ArchitectureAnalysisRequest request, byte[] content)
    {
        if (request.AnalysisRequestId == Guid.Empty)
            throw new ValidationException("Analysis request id must be a non-empty GUID.");

        if (request.RequestedByUserId == Guid.Empty)
            throw new ValidationException("Requested by user id must be a non-empty GUID.");

        if (string.IsNullOrWhiteSpace(request.SourceFileName))
            throw new ValidationException("Source file name is required.");

        if (string.IsNullOrWhiteSpace(request.ContentType))
            throw new ValidationException("Content type is required.");

        if (!SupportedContentTypes.Contains(request.ContentType))
            throw new UnsupportedDiagramFormatException(request.ContentType);

        if (request.DiagramType is not (DiagramType.Pdf or DiagramType.Image))
            throw new UnsupportedDiagramFormatException(request.ContentType);

        if (content.Length < _options.MinFileSizeInBytes)
            throw new DiagramProcessingException($"The source file is too small for AI analysis. Minimum: {_options.MinFileSizeInBytes} bytes. Provided: {content.Length} bytes.");

        if (content.Length > _options.MaxFileSizeInBytes)
            throw new DiagramProcessingException($"The source file exceeds the maximum size allowed for AI analysis. Maximum: {_options.MaxFileSizeInBytes} bytes. Provided: {content.Length} bytes.");

        if (!_signatureValidator.IsSignatureCompatible(request.DiagramType, content))
        {
            var expected = _signatureValidator.ExpectedSignatureDescription(request.DiagramType);
            throw new DiagramProcessingException($"The source file signature does not match the declared diagram type '{request.DiagramType}'. Expected: {expected}.");
        }
    }
}
