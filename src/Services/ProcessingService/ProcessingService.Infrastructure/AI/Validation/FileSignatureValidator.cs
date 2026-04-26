using ProcessingService.Domain.Enums;

namespace ProcessingService.Infrastructure.AI.Validation;

public sealed class FileSignatureValidator
{
    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();
    private static readonly byte[] PngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
    private static readonly byte[] JpegSignature = new byte[] { 0xFF, 0xD8, 0xFF };

    public bool IsSignatureCompatible(DiagramType diagramType, ReadOnlySpan<byte> bytes)
    {
        return diagramType switch
        {
            DiagramType.Pdf => StartsWith(bytes, PdfSignature),
            DiagramType.Image => StartsWith(bytes, PngSignature) || StartsWith(bytes, JpegSignature),
            _ => false
        };
    }

    public string ExpectedSignatureDescription(DiagramType diagramType)
    {
        return diagramType switch
        {
            DiagramType.Pdf => "%PDF",
            DiagramType.Image => "PNG or JPEG magic number",
            _ => "unsupported file signature"
        };
    }

    private static bool StartsWith(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> expected)
    {
        return bytes.Length >= expected.Length && bytes[..expected.Length].SequenceEqual(expected);
    }
}
