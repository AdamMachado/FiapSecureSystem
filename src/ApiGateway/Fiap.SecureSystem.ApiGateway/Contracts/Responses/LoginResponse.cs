namespace Fiap.SecureSystem.ApiGateway.Contracts.Responses;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    LoginUserResponse User);
