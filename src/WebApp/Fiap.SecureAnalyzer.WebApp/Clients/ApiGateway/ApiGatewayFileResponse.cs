namespace Fiap.SecureAnalyzer.WebApp.Clients.ApiGateway;

public sealed record ApiGatewayFileResponse(
    byte[] Content,
    string ContentType,
    string FileName);
