using Microsoft.Extensions.Options;
using ProcessingService.Domain.Enums;
using ProcessingService.Domain.Exceptions;
using ProcessingService.Infrastructure.AI.Inspection;
using ProcessingService.Infrastructure.AI.Options;

namespace ProcessingService.Infrastructure.AI.Policies;

public sealed class AiInputCostPolicy
{
    private readonly ArchitectureAnalysisOptions _options;

    public AiInputCostPolicy(IOptions<ArchitectureAnalysisOptions> options)
    {
        _options = options.Value;
    }

    public void ValidateAndThrow(AnalysisFileInspectionResult inspection)
    {
        if (inspection.SizeInBytes > _options.MaxFileSizeInBytes)
            throw new DiagramProcessingException($"The file exceeds the AI input cost policy size limit. Maximum: {_options.MaxFileSizeInBytes} bytes. Provided: {inspection.SizeInBytes} bytes.");

        if (inspection.DiagramType == DiagramType.Pdf && inspection.PageCount > _options.MaxPdfPages)
            throw new DiagramProcessingException($"The PDF exceeds the AI input cost policy page limit. Maximum: {_options.MaxPdfPages}. Provided: {inspection.PageCount}.");

        if (inspection.Width > _options.MaxImageWidth || inspection.Height > _options.MaxImageHeight)
            throw new DiagramProcessingException($"The image exceeds the AI input cost policy dimension limit. Maximum: {_options.MaxImageWidth}x{_options.MaxImageHeight}. Provided: {inspection.Width}x{inspection.Height}.");
    }
}
