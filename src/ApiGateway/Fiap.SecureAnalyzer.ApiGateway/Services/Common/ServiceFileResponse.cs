namespace Fiap.SecureAnalyzer.ApiGateway.Services.Common;

public sealed record ServiceFileResponse(
    byte[] Content,
    string ContentType,
    string? FileName);
