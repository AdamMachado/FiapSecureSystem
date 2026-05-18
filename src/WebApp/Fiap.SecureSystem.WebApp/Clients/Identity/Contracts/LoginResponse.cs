namespace Fiap.SecureSystem.WebApp.Clients.Identity.Contracts;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    LoginUserResponse User);
